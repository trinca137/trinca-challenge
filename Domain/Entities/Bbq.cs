using System;
using Domain.Events;

namespace Domain.Entities
{
    public class Bbq : AggregateRoot
    {
        public string Reason { get; set; }
        public BbqStatus Status { get; set; }
        public DateTime Date { get; set; }
        public bool IsTrincasPaying { get; set; }
        public int BbqConfirmation { get; set; }
        public ShoppingList ShoppingListBbq { get; set; } = new ShoppingList();


        public void When(ThereIsSomeoneElseInTheMood @event)
        {
            Id = @event.Id.ToString();
            Date = @event.Date;
            Reason = @event.Reason;
            Status = BbqStatus.New;
        }

        public void When(BbqStatusUpdated @event)
        {
            if (@event.GonnaHappen)
                Status = BbqStatus.PendingConfirmations;
            else 
                Status = BbqStatus.ItsNotGonnaHappen;

            if (@event.TrincaWillPay)
                IsTrincasPaying = true;
        }

        public void When(InviteWasDeclined @event)
        {
            //TODO:Deve ser possível rejeitar um convite já aceito antes.
            //Se este for o caso, a quantidade de comida calculada pelo aceite anterior do convite
            //deve ser retirado da lista de compras do churrasco.
            BbqConfirmation -= 1;
            var veg = @event.IsVeg ? -0.6 : -0.3;
            var meat = @event.IsVeg ? 0 : -0.3;
            ShoppingListBbq.AtualizarLista(veg, meat);

            //Se ao rejeitar, o número de pessoas confirmadas no churrasco for menor que sete,
            //o churrasco deverá ter seu status atualizado para “Pendente de confirmações”.
            if (BbqConfirmation < 7 && Status != BbqStatus.PendingConfirmations)
                Status = BbqStatus.PendingConfirmations;
        }

        public void When(InviteWasAccepted @event)
        {
            BbqConfirmation += 1;

            var veg = @event.IsVeg ? 0.6 : 0.3;
            var meat = @event.IsVeg ? 0 : 0.3;
            ShoppingListBbq.AtualizarLista(veg, meat);
        }

        public void When(BbqConfirmed @event)
        {
            Status = @event.Confirmed ? BbqStatus.Confirmed : BbqStatus.PendingConfirmations;
        }

        public object TakeSnapshot()
        {
            return new
            {
                Id,
                Date,
                IsTrincasPaying,
                Status = Status.ToString(),
                BbqConfirmation,
                ShoppingListBbq
            };
        }
    }
}
