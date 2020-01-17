﻿using DAL;
using BE;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL
{
    public class ImpBL : IBL
    {

        #region Singleton
        private static readonly ImpBL instance = new ImpBL();
        //Using Singleton makes sure that no new instance of the class is ever created but only one instance.
        public static ImpBL Instance
        {
            get { return instance; }
        }

        #endregion

        static IDAL dal;

        static ImpBL()
        {
            string TypeDAL = ConfigurationSettings.AppSettings.Get("TypeDS");
            //string TypeDAL = "List";
            dal = factoryDAL.GetDAL(TypeDAL);
        }

        

        #region Gusets Requsts

        public GuestRequest searchByKey(List<GuestRequest> guestRequest, int key = -1)
        {

            foreach (var item in guestRequest)
            {
                if (item.GuestRequestKey == key)
                {
                    return item;
                }
            }
            return null;
        }

        public List<GuestRequest> searchByName(string familyName)
        {
            return GetGuestRequestList()
                .Where(x=>x.FamilyName.ToLower().StartsWith(familyName.ToLower())).ToList();
        }

        public GuestRequest GetRequest(int id)
        {
            return getGuestRequestIfExists(id);
        }

        public List<HostingUnit> GetUnitsByHost(string hostId)
        {
            return GetUnitsList().Where(x => x.Owner.HostId == hostId).ToList();
        }

        public List<GuestRequest> matchRequestToUnit(HostingUnit unit)
        {
            return GetGuestRequestList().Where(grItem => checkIfUnitMatchToRequest(unit, grItem) != null).ToList();
        }

        public GuestRequest checkIfUnitMatchToRequest(HostingUnit hu, GuestRequest gr)
        {
            // if the guest already have order
            if(GetOrdersList().Any(o=>o.GuestRequestKey==gr.GuestRequestKey && o.HostingUnitKey == hu.HostingUnitKey))
            {
                return null;
            }
            if(
                hu.Area == gr.Area
                && hu.Type == gr.Type
                && isDatesAvilable(hu, gr.EntryDate, gr.ReleaseDate)
                && isMatchRequirment(hu.Pool, gr.Pool)
                && isMatchRequirment(hu.Jacuzz, gr.Jacuzzi)
                && isMatchRequirment(hu.Garden, gr.Garden)
                && isMatchRequirment(hu.ChildrensAttractions, gr.ChildrensAttractions)
                && isMatchRequirment(hu.breakfastIncluded, gr.breakfastIncluded)
                && isMatchRequirment(hu.FreeParking, gr.FreeParking)
                && isMatchRequirment(hu.RoomService, gr.RoomService)
                )
            {
                return gr;
            }

            return null;
        }

        bool isMatchRequirment(bool hasOption, Options requrement)
        {
            return !(!hasOption && requrement == Options.neccesery);
        }

        public List<List<GuestRequest>> GroupRequesteByStatus()
        {
            return (from gr in GetGuestRequestList()
                    group gr by gr.Status into g
                    select g.ToList()).ToList();

        }

        public Host GetHost(string hostId)
        {
            var hu = GetUnitsList().FirstOrDefault(x => x.Owner.HostId == hostId);
            if (hu != null)
            {
                return hu.Owner;
            }
            else
            {
                throw new TzimerException($"Sorry, cannot find host with ID: {hostId}", "bl");
            }
        }

        public void AddRequest(GuestRequest newRequest)
        {
            validGuestRequest(newRequest);

            if (newRequest.Status != RequestStatus.Open)
            {
                throw new TzimerException("Sorry, new request must be in status - 'open'", "bl");
            }
            dal.AddRequest(newRequest);
        }

        public void UpdateRequest(GuestRequest updatedRequest)
        {
            validGuestRequest(updatedRequest);

            GuestRequest oldReq = getGuestRequestIfExists(updatedRequest.GuestRequestKey);

            if (oldReq.Status == RequestStatus.ClosedDeal || oldReq.Status == RequestStatus.ExpiredRequest)
            {
                throw new TzimerException($"Cannot update a closed request, Request ID: {updatedRequest.GuestRequestKey}", "bl");
            }

            dal.UpdateRequest(updatedRequest);
        }

        static Func<int, GuestRequest> getGuestRequestIfExists = delegate (int id)//A delegate function that accepts any guest request and checks by its ID whether it already exists, if it does not throw an exception, else returns the request.
        {
            var oldReq = dal.GetRequest(id);
            if (oldReq == null)
            {
                throw new TzimerException($"Cannot find Requst with ID: {id}", "bl");
            }
            return oldReq;
        };

        public void DeleteRequest(GuestRequest request)
        {
            getGuestRequestIfExists(request.GuestRequestKey);
            dal.DeleteRequest(request);
        }

        private void validGuestRequest(GuestRequest newRequest)//Function that checks the integrity of the guest request.
        {
            //Check that the vacation start date is at least one day earlier than the vacation end date.
            if ((newRequest.ReleaseDate - newRequest.EntryDate).TotalDays < 1)
            {
                throw new TzimerException("Sorry, the dates you chose are invalid, entry must be before leave!", "bl");
            }
            //If one of the fields is empty, you will send an exception and request a refill.
            if (string.IsNullOrEmpty(newRequest.FamilyName))
            {
                throw new TzimerException("Please enter your family name", "bl");
            }
            if (string.IsNullOrEmpty(newRequest.PrivateName))
            {
                throw new TzimerException("Please enter your private name", "bl");
            }
            if (string.IsNullOrEmpty(newRequest.MailAddress))
            {
                throw new TzimerException("Please enter your e-mail address", "bl");

            }
            if (string.IsNullOrEmpty(newRequest.PhoneNumber))
            {
                throw new TzimerException("Please enter your phone number", "bl");
            }

            if (!(newRequest.MailAddress.Contains("@")))
            {
                throw new TzimerException("E-mail Address format is invaled.Please enter the correct format.", "bl");
            }
            if (string.IsNullOrEmpty(newRequest.Children))
            {
                throw new TzimerException("Please enter the amount of children", "bl");

            }
            if (string.IsNullOrEmpty(newRequest.Adults))
            {
                throw new TzimerException("Please enter the amount of adults", "bl");

            }
            int number;
            bool checknumber = Int32.TryParse(newRequest.PhoneNumber, out number);
            if (!checknumber)
            {
                throw new TzimerException("Phone number may contain only numbers.", "bl");

            }
            //If the number of adults is equal to 0, an exception is sent.
            if (newRequest.Adults == "0")
            {
                throw new TzimerException("Cannot set request with zero adults!", "bl");
            }
        }

        #endregion

        #region Hosting Units

        public HostingUnit searchByKey(List<HostingUnit> hostingUnit, int key = -1)
        {
            foreach (var item in hostingUnit)
            {
                if (item.HostingUnitKey == key)
                {
                    return item;
                }
            }
            throw new TzimerException($"Sorry,cant find a request with the key{key}", "bl");
        }

        public List<HostingUnit> searchByName(List<HostingUnit> HostingUnit, string Name)
        {
            bool ifExist = false;
            List<HostingUnit> nameHU = new List<HostingUnit>();
            foreach (var item in HostingUnit)
            {
                if (item.HostingUnitName == Name)
                {
                    nameHU.Add(item);
                    ifExist = true;
                }

            }


            if (ifExist)
            {
                return nameHU;
            }

            else
                throw new TzimerException($"Sorry,cant find a request with the name:{Name}", "bl");
        }

        static Func<int, HostingUnit> getHostingUnitsIfExists = delegate (int id)//A delegate function that accepts any hosting unit and checks by its ID whether it already exists, if it does not throw an exception, else returns the unit.
        {
            var oldUnit = dal.GetUnit(id);
            if (oldUnit == null)
            {
                throw new TzimerException($"Cannot find Unit with ID: {id}", "bl");
            }
            return oldUnit;
        };

        public HostingUnit GetUnit(int id)
        {
            return getHostingUnitsIfExists(id);
        }

        public void AddUnit(HostingUnit newUnit)
        {

            validHostingUnit(newUnit);

            dal.AddUnit(newUnit);
        }

        public void UpdateUnit(HostingUnit updatedUnit)
        {
            getHostingUnitsIfExists(updatedUnit.HostingUnitKey);
            dal.UpdateUnit(updatedUnit);
        }

        public void DeleteUnit(HostingUnit delUnit)
        {
            getHostingUnitsIfExists(delUnit.HostingUnitKey);
            if (isHaveOpenOrder(delUnit))
            {
                throw new TzimerException("Cannot delete Host Unit that has active deals", "bl");
            }
            dal.DeleteUnit(delUnit);
        }

        private static bool isHaveOpenOrder(HostingUnit unit)//A function that goes through the order list and checks for an open order.
        {
            return dal.GetOrdersList().Any(x => x.HostingUnitKey == unit.HostingUnitKey && (x.Status == OrderStatus.NotHandled || x.Status == OrderStatus.SentMail));
        }

        private void validHostingUnit(HostingUnit newUnit)//If the hosting unit does not have a name - we will throw an error asking you to re-enter the unit name.
        {
            if (string.IsNullOrEmpty(newUnit.HostingUnitName))
            {
                throw new TzimerException("Please enter your Host name", "bl");
            }
        }

        public List<HostingUnit> GetUnitsList()
        {
            return dal.GetUnitsList();
        }

        #endregion

        #region Orders

        public Order searchByKey(List<Order> order, int key = -1)
        {
            foreach (var item in order)
            {
                if (item.OrderKey == key)
                {
                    return item;
                }
            }
            return null;
        }

        static Func<int, Order> getOrderIfExists = delegate (int id)
        {
            var oldOrder = dal.GetOrder(id);
            if (oldOrder == null)
            {
                throw new TzimerException($"Cannot find Order with ID: {id}", "bl");
            }
            return oldOrder;
        };

        public Order GetOrder(int id)
        {
            return getOrderIfExists(id);
        }

        public void AddOrder(Order newOrder)
        {
            var req = getGuestRequestIfExists(newOrder.GuestRequestKey);
            var unit = getHostingUnitsIfExists(newOrder.HostingUnitKey);
            if (!AvailableDate(unit, req))
            {
                throw new TzimerException("Sorry, those dates are taken, please choose a new date", "bl");
            }
            dal.AddOrder(newOrder);
        }

        public void UpdateOrder(Order updatedOrder)
        {
            var oldOrder = getOrderIfExists(updatedOrder.OrderKey);
            var req = getGuestRequestIfExists(updatedOrder.GuestRequestKey);
            var unit = getHostingUnitsIfExists(updatedOrder.HostingUnitKey);
            if (oldOrder.Status == OrderStatus.Canceled || oldOrder.Status == OrderStatus.DoneDeal)
            {
                throw new TzimerException("Sorry, this order already closed", "bl");
            }
            if (updatedOrder.Status == OrderStatus.DoneDeal)
            {
                var totalDays = (req.ReleaseDate - req.EntryDate).TotalDays;
                Configuration.Profits += (totalDays * Configuration.Commissin);
                updateDatesAvilable(unit, req);
                cancelAllOtherOrders(updatedOrder);
                req.Status = RequestStatus.ClosedDeal;
            } 
            if (updatedOrder.Status == OrderStatus.Canceled)
            {
                req.Status = RequestStatus.ExpiredRequest;
            }
            if (updatedOrder.Status == OrderStatus.NotHandled)
            {
                req.Status = RequestStatus.Open;
            }
            UpdateRequest(req);
            dal.UpdateOrder(updatedOrder);
        }

        #endregion


        public List<Order> GetOrdersByUnit(int hostingUnitKey)
        {
            return GetOrdersList().Where(x => x.HostingUnitKey == hostingUnitKey).ToList();
        }

        public List<HostingUnit> GetAllAvilableUnits(DateTime start, int amountOfDAys)
        {
            DateTime end = start.AddDays(amountOfDAys);
            return GetUnitsList().Where(x => isDatesAvilable(x, start, end)).ToList();
        }

        void updateDatesAvilable(HostingUnit unit, GuestRequest req)//If a vacant unit is on a particular date and the guest wants to be in this unit - the matrix will change the days in the unit to be occupied.
        {
            DateTime tempDate = req.EntryDate;
            while (tempDate < req.ReleaseDate)
            {
                unit.Diary[tempDate.Month - 1, tempDate.Day - 1] = true;
                tempDate = tempDate.AddDays(1);
            }
        }

        static void cancelAllOtherOrders(Order order)
        {
            dal.GetOrdersList().ForEach(o =>
            {
                if (o.GuestRequestKey == order.GuestRequestKey && o.OrderKey != order.OrderKey)
                {
                    o.Status = OrderStatus.Canceled;
                    dal.UpdateOrder(o);
                }
            });
        }

        public List<GuestRequest> GetGuestRequestList()
        {
            return dal.GetGuestRequestList();
        }

        public List<Order> GetOrdersList()
        {
            return dal.GetOrdersList();
        }

        public List<Host> GetHostsList()
        {
            return dal.GetHostList();
        }

        public List<BankBranch> GetBankList()
        {
            return dal.GetBankList();
        }

        public bool SendOrder(Host h, Order o)
        {
            if (h.CollectionClearance)
            {
                // TODO: send request in SMTP to Mail server


                o.Status = OrderStatus.SentMail;
                UpdateOrder(o);
                return true;
            }
            return false;
        }



        bool isDatesAvilable(HostingUnit unit, DateTime start, DateTime end)//A function that checks for a particular unit is available on certain dates.
        {
            DateTime tempDate = start;
            while (tempDate < end)
            {
                if (unit.Diary[tempDate.Month - 1, tempDate.Day - 1])
                {
                    return false;
                }

                tempDate = tempDate.AddDays(1);
            }
            return true;
        }

        bool availableDate(HostingUnit h, DateTime start, int amount)
        {
            DateTime end = start.AddDays(amount);
            return isDatesAvilable(h, start, end);
        }

        public double AmountOfDays(DateTime start, DateTime end)
        {
            end = end == null ? DateTime.Now : end;
            return (end - start).TotalDays;
        }

        public bool AvailableDate(HostingUnit h, GuestRequest g)
        {
            return isDatesAvilable(h, g.EntryDate, g.ReleaseDate);
        }

        public List<GuestRequest> GetAllGuestRequest(Predicate<GuestRequest> condition)
        {
            return GetGuestRequestList().Where(x => condition(x)).ToList();
        }

        public int GetNumOfOrders(GuestRequest gr)
        {
            return GetOrdersList().Where(x => x.GuestRequestKey == gr.GuestRequestKey).Count();
        }

        public int GetNumOfSentOrders(HostingUnit hu)
        {
            return GetOrdersList().Where(x => x.HostingUnitKey == hu.HostingUnitKey &&
            (x.Status == OrderStatus.SentMail || x.Status == OrderStatus.DoneDeal)).Count();
        }

        Func<Order, int, bool> isOldOrder = delegate (Order order, int amountOfDays)//A function that accepts several days, and returns all orders that have elapsed since they were created / since the email was sent to a customer greater than or equal to the number of days the function received.
        {
            DateTime start = order.OrderDate > order.CreateDate ? order.CreateDate : order.OrderDate;
            DateTime end = DateTime.Now;
            return (end - start).TotalDays >= amountOfDays;
        };

        public List<Order> GetOldOrders(int amountOfDays)
        {
            return GetOrdersList().Where(x => isOldOrder(x, amountOfDays)).ToList();
        }

        int amountOfOrders(HostingUnit h, List<Order> orderList)//A function that goes through the order list and checks for each unit how many orders it has closed.
        {
            int sum = 0;
            foreach (var item in orderList)
            {
                if (item.HostingUnitKey == h.HostingUnitKey)
                {
                    if (item.Status == OrderStatus.Canceled || item.Status == OrderStatus.DoneDeal)
                    {
                        sum++;
                    }
                }
            }
            return sum;
        }

        public List<List<GuestRequest>> GroupGuestRequestByAreas()
        {
            return (from gr in GetGuestRequestList()
                    group gr by gr.Area into g
                    select g.ToList()).ToList();

        }

        public List<List<HostingUnit>> GroupHostingUnitsByArea()
        {
            return (from hu in GetUnitsList()
                    group hu by hu.Area into g
                    select g.ToList()).ToList();

        }

        public List<List<GuestRequest>> GroupGuestRequestByNumOfAtendees()
        {
            return (from gr in GetGuestRequestList()
                    group gr by gr.Children + gr.Adults into g
                    select g.ToList()).ToList();

        }

        public List<List<Host>> GroupHostsByNumOfUnits()
        {
            return (from h in getHostsList()
                    group h by getNumOfUnits(h.HostId) into g
                    select g.ToList()).ToList();
        }

        public int getNumOfUnits(string hostID)//A function that returns the number of units each host has.
        {
            List<Host> myhostlist = getHostsList();
            int sum = 0;
            foreach (var item in myhostlist)
            {
                if(hostID==item.HostId)
                {
                    sum= GetUnitsList().Sum(x => x.Owner.HostId == item.HostId ? 1 : 0);
                }
            }
            return sum;
        }

        private List<Host> getHostsList()
        {
            var hostIds = GetUnitsList().Select(x => x.Owner.HostId).Distinct().ToList();
            return hostIds.Select(x => (Host)GetHost(x).Clone()).ToList();
        }

        public void DeleteOrder(Order update)
        {

        }

        public List<List<GuestRequest>> GroupRequestByStatus()
        {
            return (from gr in GetGuestRequestList()
                    group gr by gr.Status into g
                    select g.ToList()).ToList();
        }

        public List<Tuple<DateTime, DateTime>> markTakenDatesInMatrix(HostingUnit unit)
        {
            List<Tuple<DateTime, DateTime>> res = new List<Tuple<DateTime, DateTime>>();
            var allReleventOrders = GetOrdersByUnit(unit.HostingUnitKey)
                .Where(order => order.Status == OrderStatus.DoneDeal)
                .Select(x => x.GuestRequestKey);
            return GetGuestRequestList().Where(gr => allReleventOrders.Contains(gr.GuestRequestKey))
                .Select(item => new Tuple<DateTime, DateTime>(item.EntryDate, item.ReleaseDate)).ToList();

        }

        public int GetNumberOfTakenDays(HostingUnit hostingUnit)// Function who returns the total number of busy days per year for one hosting unit.
        {
            int counter = 0;
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    if (hostingUnit.Diary[i, j])
                    {
                        counter++;
                    }
                }
            }
            return counter;
        }

        public float GetAnnualBusyPercentage(string UnitName)//A function that returns the percentage of annual occupancy for one hosting unit.
        {
            double precent = 0.0, counter=0.0;
            List<HostingUnit> mylist =  GetUnitsList();
            foreach (var item in mylist)
            {
                if (UnitName == item.HostingUnitName)
                {
                    counter = GetNumberOfTakenDays(item);
                    precent = (counter / 365) * (100);
                }
            }
            return (float)precent;
        }

        public float GetAnnualBusyPercentageForAllUnitsForOneHost(string HostID)//A function that returns the percentage of annual occupancy for all the hosting unit that one host has.
        {
            List<Host> myHostList = GetHostsList();
            float sum = 0, counter = 0, precent = 0;
            foreach (var item in myHostList)
            {
                if (HostID == (item.HostId))
                {
                    List<HostingUnit> listhu = GetUnitsByHost(item.HostId);
                    foreach (var itemm in listhu)
                    {
                        counter = GetAnnualBusyPercentage(itemm.HostingUnitName);
                        sum += counter;
                    }
                    precent = (sum / 365) * (100);
                    
                }
            }
            return precent;
        }

        public float GetAnnualBusyPercentageForAllUnitsForTheAdministor()//A function that returns the percentage of annual occupancy for all the hosting unit that adminisrot has.
        {
            float sum = 0, precent = 0;
            List<Host> listH = GetHostsList();
            foreach (var item in listH)
            {
                float counter = GetAnnualBusyPercentageForAllUnitsForOneHost(item.HostId);
                sum += counter;
            }
            precent = (sum / 365) * (100);
            return precent;
        }

        public int getOverallNumOfUnints()
        {
            return GetUnitsList().Count;

        }

        public List<HostingUnit> groupHostingUnitsByRates()
        {
            List<HostingUnit> lhu = new List<HostingUnit>();
            foreach (var item in GetUnitsList())
            {
                var x = from newItem in GetUnitsList()
                        orderby newItem.RateStars
                        select newItem;
                lhu = x.ToList();
            }
            return lhu;
        }

        public List<List<Order>> GroupOrdersByStatus()
        {
            return (from o in GetOrdersList()
                    group o by o.Status into g
                    select g.ToList()).ToList();

        }
    }

}
