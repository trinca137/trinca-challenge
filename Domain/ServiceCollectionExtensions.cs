﻿using System;
using Eveneum;
using System.Linq;
using CrossCutting;
using Domain.Entities;
using System.Threading.Tasks;
using Domain.Events;
using Domain.Repositories;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Domain.Interfaces;
using Domain.Services;

namespace Domain
{
    public static partial class ServiceCollectionExtensions
    {
        private const string DATABASE = "Churras";
        public static IServiceCollection AddDomainDependencies(this IServiceCollection services)
            => services.AddSingleton(new Person { Id = "e5c7c990-7d75-4445-b5a2-700df354a6a0" })
                .AddEventStoreDependencies()
                .AddRepositoriesDependencies();

        public static IServiceCollection AddEventStoreDependencies(this IServiceCollection services)
        {
            var client = new CosmosClient(Environment.GetEnvironmentVariable(nameof(EventStore)));

            var bbqStore = new EventStore<Bbq>(client, DATABASE, "Bbqs");
            bbqStore.Initialize().GetAwaiter().GetResult();

            var peopleStore = new EventStore<Person>(client, DATABASE, "People");
            peopleStore.Initialize().GetAwaiter().GetResult();

            var snapshots = new SnapshotStore(client.GetDatabase(DATABASE));

            client.GetDatabase(DATABASE)
                .GetContainer("Lookups")
                .UpsertItemAsync(new Lookups { PeopleIds = Data.People.Select(o => o.Id).ToList(), ModeratorIds = Data.People.Where(p => p.IsCoOwner).Select(o => o.Id).ToList() })
                .GetAwaiter()
                .GetResult();

            try
            {
                foreach (var person in Data.People)
                {
                    peopleStore.WriteToStream(person.Id, new[] { new EventData(person.Id, new PersonHasBeenCreated(person.Id, person.Name, person.IsCoOwner), null, 0, DateTime.Now.ToString()) })
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("skipping already included data.");
            }

            services.AddSingleton(snapshots);

            services.AddSingleton<IEventStore<Bbq>>(bbqStore);
            services.AddSingleton<IEventStore<Person>>(peopleStore);
            services.AddScoped<IBbqService, BbqService>();
            services.AddScoped<IInvitesService, InvitesService>();

            return services;
        }

        public static IServiceCollection AddRepositoriesDependencies(this IServiceCollection services)
            => services.AddTransient<IBbqRepository, BbqRepository>()
            .AddTransient<IPersonRepository, PersonRepository>();

        private async static Task CreateIfNotExists(this CosmosClient client, string database, string collection)
        {
            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(database);
            await databaseResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(collection, "/StreamId"));
        }
    }

    public static class Data
    {
        public static List<Person> People => new List<Person>
        {
            new Person { Id = "e5c7c990-7d75-4445-b5a2-700df354a6a0", Name = "João da Silva", IsCoOwner = false },
            new Person { Id = "171f9858-ddb1-4adf-886b-2ea36e0f0644", Name = "Marcos Oliveira", IsCoOwner = true },
            new Person { Id = "3f74e6bd-11b2-4d48-a294-239a7a2ce7d5", Name = "Gustavo Sanfoninha", IsCoOwner = true },
            new Person { Id = "795fc8f2-1473-4f19-b33e-ade1a42ed123", Name = "Alexandre Morales", IsCoOwner = false },
            new Person { Id = "addd0967-6e16-4328-bab1-eec63bf31968", Name = "Leandro Espera", IsCoOwner = false }
        };
    }
}
