using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunModerateBbq
    {
        private readonly SnapshotStore _snapshots;
        private readonly IPersonRepository _persons;
        private readonly IBbqRepository _repository;

        public RunModerateBbq(IBbqRepository repository, SnapshotStore snapshots, IPersonRepository persons)
        {
            _persons = persons;
            _snapshots = snapshots;
            _repository = repository;
        }

        [Function(nameof(RunModerateBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "churras/{id}/moderar")] HttpRequestData req, string id)
        {
            var moderationRequest = await req.Body<ModerateBbqRequest>();

            var bbq = await _repository.GetAsync(id);

            //Tá criando um novo evento, sem verificar se o churrasco de fato existe
            bbq.Apply(new BbqStatusUpdated(moderationRequest.GonnaHappen, moderationRequest.TrincaWillPay));

            var lookups = await _snapshots.AsQueryable<Lookups>("Lookups").SingleOrDefaultAsync();

            await _repository.SaveAsync(bbq);

            lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));

            if (moderationRequest.GonnaHappen)
            {
                lookups.PeopleIds.RemoveAll(item => lookups.ModeratorIds.Contains(item));
                foreach (var personId in lookups.PeopleIds)
                {
                    var person = await _persons.GetAsync(personId);
                    var @event = new PersonHasBeenInvitedToBbq(bbq.Id, bbq.Date, bbq.Reason);
                    person.Apply(@event);
                    await _persons.SaveAsync(person);
                }
            }
            else
            {
                foreach (var personId in lookups.ModeratorIds)
                {
                    var person = await _persons.GetAsync(personId);
                    person.Apply(new InviteWasDeclined { InviteId = id, PersonId = person.Id });
                    await _persons.SaveAsync(person);
                }
            }

            return await req.CreateResponse(System.Net.HttpStatusCode.OK, bbq.TakeSnapshot());
        }
    }
}
