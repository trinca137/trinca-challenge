using Domain.Events;
using System;

namespace Domain.Entities
{
    public class ShoppingList
    {
        public double Vegetables { get; set; }
        public double Meat { get; set; }
        public void AtualizarLista(double vegetables, double meat)
        {
            Vegetables += vegetables;
            Meat += meat;
        }
    }
}
