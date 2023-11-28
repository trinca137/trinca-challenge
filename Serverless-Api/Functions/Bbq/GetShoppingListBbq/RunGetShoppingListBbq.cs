using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Serverless_Api
{
    public partial class RunGetShoppingListBbq
    {
        private readonly Person _user;
        private readonly IBbqRepository _bbqRepository;
        private readonly IPersonRepository _personsRepository;
        public RunGetShoppingListBbq(IBbqRepository bbqRepository, Person user, IPersonRepository personsRepository)
        {
            _user = user;
            _bbqRepository = bbqRepository;
            _personsRepository = personsRepository;
        }

        [Function(nameof(RunGetShoppingListBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "churras/{id}/lista-compras")] HttpRequestData req, string id)
        {
            var snapshots = new List<object>();
            var person = await _personsRepository.GetAsync(_user.Id);

            if (person == null)
                return req.CreateResponse(HttpStatusCode.NoContent);

            if (!person.IsCoOwner)
                return req.CreateResponse(HttpStatusCode.Unauthorized);

            var bbq = await _bbqRepository.GetAsync(id);

            if (bbq == null)
                return req.CreateResponse(HttpStatusCode.NoContent);

            var details = new
            {
                Motivo = bbq.Reason,
                Data = bbq.Date.ToString("dd/MM/yyyy HH:mm:ss"),
                Status = bbq.Status.ToString(),
                TotalVegetais = $"{bbq.ShoppingListBbq.Vegetables:N3} kg",
                TotalCarne = $"{bbq.ShoppingListBbq.Meat:N3} kg"
            };

            return await req.CreateResponse(HttpStatusCode.Created, details);
        }
    }
}
