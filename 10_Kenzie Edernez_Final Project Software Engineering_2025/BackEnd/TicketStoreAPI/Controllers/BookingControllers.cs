using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketStoreAPI.Data;
using TicketStoreAPI.Models;
using TicketStoreAPI.Models.request;
using TicketStoreAPI.Models.Response;

namespace TicketStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly TicketStoreAPIContext _context;

        public BookingsController(TicketStoreAPIContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<ResponseModel<object>>> GetBookings()
        {
            var userIdString = User.FindFirstValue("name");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Please login again."
                });
            }

            var bookings = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Theater)
                .Include(b => b.User)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .Where(c => c.UserId == userIdString)
                .ToListAsync();
            List<BookingResponse> response = new List<BookingResponse>();

            if (bookings == null || !bookings.Any())
            {
                return NotFound(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    RequestMethod = HttpContext.Request.Method,
                    Data = "No bookings found."
                });
            }

            foreach (Booking b in bookings)
            {
                var temp = new BookingResponse
                {
                    BookingId = b.BookingId,
                    BookingStatus = b.BookingStatus,
                    CreatedAt = b.CreatedAt,
                    ScheduleId = b.ScheduleId,
                    TotalPrice = b.TotalPrice,
                    UserId = b.UserId
                };
                response.Add(temp);
            }


            return Ok(new ResponseModel<List<BookingResponse>>
            {
                StatusCode = StatusCodes.Status200OK,
                RequestMethod = HttpContext.Request.Method,
                Data = response
            });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ResponseModel<object>>> GetBooking(int id)
        {
            var userIdString = User.FindFirstValue("name");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Please login again."
                });
            }

            var booking = await _context.Bookings
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Movie)
                .Include(b => b.Schedule)
                    .ThenInclude(s => s.Theater)
                .Include(b => b.User)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Booking with ID {id} not found."
                });
            }

            if (booking.UserId.ToString() != userIdString)
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Unauthorized Access to Booking with ID {id}."
                });
            }

            var response = new BookingResponse
            {
                BookingId = booking.BookingId,
                BookingStatus = booking.BookingStatus,
                CreatedAt = booking.CreatedAt,
                ScheduleId = booking.ScheduleId,
                TotalPrice = booking.TotalPrice,
                UserId = booking.UserId
            };

            return Ok(new ResponseModel<BookingResponse>
            {
                StatusCode = StatusCodes.Status200OK,
                RequestMethod = HttpContext.Request.Method,
                Data = response
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ResponseModel<Booking>>> PostBooking(BookingCreateDTO bookingCreateDto)
        {
            var userIdString = User.FindFirstValue("name");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Please login again."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    RequestMethod = HttpContext.Request.Method,
                    Data = "Invalid model state."
                });
            }

            if (!await _context.Schedules.AnyAsync(s => s.ScheduleId == bookingCreateDto.ScheduleId))
            {
                return BadRequest(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Schedule with ID {bookingCreateDto.ScheduleId} does not exist."
                });
            }

            if (!await _context.Users.AnyAsync(u => u.Id == bookingCreateDto.UserId))
            {
                return BadRequest(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"User with ID {bookingCreateDto.UserId} does not exist."
                });
            }

            if (bookingCreateDto.UserId != userIdString)
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"UserId doesn't match with current user."
                });
            }

            var booking = new Booking
            {
                ScheduleId = bookingCreateDto.ScheduleId,
                UserId = bookingCreateDto.UserId,
                TotalPrice = bookingCreateDto.TotalPrice,
                BookingStatus = bookingCreateDto.BookingStatus,
                CreatedAt = DateTime.UtcNow
            };


            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new ResponseModel<string>
            {
                StatusCode = StatusCodes.Status200OK,
                RequestMethod = HttpContext.Request.Method,
                Data = "Booking done successfully."
            });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ResponseModel<string>>> DeleteBooking(int id)
        {
            var userIdString = User.FindFirstValue("name");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Please login again."
                });
            }

            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"Booking with ID {id} not found"
                });
            }

            if (booking.UserId != userIdString)
            {
                return Unauthorized(new ResponseModel<string>
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    RequestMethod = HttpContext.Request.Method,
                    Data = $"UserId doesn't match with current user."
                });
            }

            _context.BookingSeats.RemoveRange(booking.BookingSeats);
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new ResponseModel<string>
            {
                StatusCode = StatusCodes.Status200OK,
                RequestMethod = HttpContext.Request.Method,
                Data = "Booking deleted successfully"
            });
        }
    }
}
