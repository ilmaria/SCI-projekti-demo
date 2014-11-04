using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Error
{
    public class OrderManager
    {
        public List<Order> Orders;
        bool sorted = false;
        Storage Storage
        {
            get { return App.Instance.Storage; }
        }

        public OrderManager()
        {
            Orders = new List<Order>();
        }

        public void Add(params Order[] orders)
        {
            foreach (Order order in orders)
            {
                order.State = STATE.IN_QUEUE;
                UpdateStatus(order);
                Orders.Add(order);
            }
            sorted = false;
        }

        void UpdateStatus(Order order)
        {
            foreach (var line in order.Lines)
            {
                var productKeys = Storage.GetByProductCode(line.ProductCode);
                bool productMissing = (from p in productKeys where Storage.GetProduct(p).Amount >= line.Amount select p).Count() <= 0;
                if (productMissing)
                {
                    order.State |= STATE.PRODUCT_MISSING;
                    order.State &= ~STATE.IN_QUEUE;
                    line.State |= STATE.PRODUCT_MISSING;
                    line.State &= ~STATE.IN_QUEUE;
                }
                // todo johonkin tietoon jos tuotteita puuttuu
            }
            order.Priority = CalculatePriority(order);
            sorted = false;
        }
        public void ChangeState(Order order, uint state)
        {
            if (state == STATE.COLLECTED)
            {
                order.State = STATE.COLLECTED;
                // todo lines
            }
            sorted = false;
        }
        void SortOrders()
        {
            foreach (var order in Orders)
            {
                UpdateStatus(order);
            }
            Orders = Orders.OrderBy(o => o.Priority).ToList();
            sorted = true;
        }
        public void EnsureSort()
        {
            if (sorted) return;
            SortOrders();
        }
        public bool IsOrderAvailable()
        {
            // TODO
            EnsureSort();
            return (from o in Orders where o.State.HasFlag(STATE.IN_QUEUE) select o).Count() > 0;
        }
        public Order GetNextToCollect(Vector3 workerPosition, Vector3 dropoffPosition)
        {
            sorted = false;
            EnsureSort();
            if (!IsOrderAvailable())
            {
                App.Instance.ShowMessage("Virhe : tilausta ei saatavilla");
                return null;
            }
            var order = Orders[0];
            App.Instance.OptimizeOrder(order, workerPosition, dropoffPosition);
            // todo state changes
            return order;
        }
        float CalculatePriority(Order order)
        {
            // TODO State

            //eip‰ n‰yt‰ j‰rjestyv‰n p‰iv‰m‰‰r‰n mukaan
            float priority = (DateTime.Now - order.RequestedShippingDate).Duration().Seconds;

            if (order.State.HasFlag(STATE.COLLECTED | STATE.PRODUCT_MISSING)) priority += 100000000f;
            return priority;
        }
    }
    // sijainti voisi olla loogisempi
    public static class STATE
    {
        public const uint DEFAULT = 0;
        public const uint COLLECTED = 1u << 1;
        public const uint PRODUCT_MISSING = 1u << 2;
        public const uint PACKED = 1u << 3;
        public const uint SHIPPED = 1u << 4;
        public const uint COLLECTING_STARTED = 1u << 5;
        public const uint IN_QUEUE = 1u << 6; // order waiting to be collected

        public static bool HasFlag(this uint value, uint flag)
        {
            return (value & flag) != 0;
        }
    }
}
