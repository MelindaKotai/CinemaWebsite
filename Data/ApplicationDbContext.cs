using Licenta.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

using Licenta.Services;

namespace Licenta.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Movie> Movies{ get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Prize> Prizes { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservedSeats> ReservedSeats { get; set; }
        public DbSet<Screening> Screenings { get; set; }
        public DbSet<Seat> Seats{ get; set; }
        public DbSet<UserPrizes> UserPrizes { get; set; }
        public DbSet<TicketType> TicketType { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Seed();
            base.OnModelCreating(builder);

        }
    }


}
