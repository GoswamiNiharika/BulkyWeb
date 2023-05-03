using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderViewModel OrderViewModel { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderViewModel = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderViewModel);
            
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
            orderHeader.Name = OrderViewModel.OrderHeader.Name;
            orderHeader.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;
            orderHeader.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;
            orderHeader.City = OrderViewModel.OrderHeader.City;
            orderHeader.State = OrderViewModel.OrderHeader.State;
            orderHeader.PostalCode = OrderViewModel.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.Carrier))
            {
                orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.TrackingNumber))
            {
                orderHeader.Carrier = OrderViewModel.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new {orderId = orderHeader.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderViewModel.OrderHeader.Id, OrderStatus.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;
            orderHeader.OrderStatus = OrderStatus.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if(orderHeader.PaymentStatus == OrderStatus.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderViewModel.OrderHeader.Id);
            if(orderHeader.PaymentStatus == OrderStatus.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, OrderStatus.StatusCancelled, OrderStatus.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, OrderStatus.StatusCancelled, OrderStatus.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_Pay_Now()
        {
            OrderViewModel.OrderHeader = _unitOfWork.OrderHeader
                .Get(u => u.Id == OrderViewModel.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderViewModel.OrderDetail = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == OrderViewModel.OrderHeader.Id, includeProperties: "Product");

            //stripe logic
            var domain = "https://localhost:44323/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderViewModel.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), //$20.50 => 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == OrderStatus.PaymentStatusDelayedPayment)
            {
                //order by company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, OrderStatus.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
            return View(orderHeaderId);
        }

        #region API Calls
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId ,includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == OrderStatus.PaymentStatusPending);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == OrderStatus.StatusInProcess);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == OrderStatus.StatusApproved);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == OrderStatus.StatusShipped);
                    break;
                default:
                    break;
            }
            return Json(new { data = orderHeaders });
        }
        #endregion

    }
}
