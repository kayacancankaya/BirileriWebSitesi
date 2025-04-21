using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BirileriWebSitesi.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<Catalog> Catalogs { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<RelatedProduct> RelatedProducts { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<VariantAttribute> VariantAttributes { get; set; }
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<BasketItem> BasketItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<UserAudit> UserAudits { get; set; }
        public DbSet<PaymentLog> PaymentLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subscriber>()
                         .HasKey(i => i.Id);
            modelBuilder.Entity<Catalog>()
                         .HasKey(i => i.Id);
            modelBuilder.Entity<Product>()
                         .HasKey(p => p.ProductCode);
            modelBuilder.Entity<ProductVariant>()
                         .HasKey(p => p.ProductCode);
            modelBuilder.Entity<Variant>()
                         .HasKey(c => c.VariantCode);
            modelBuilder.Entity<VariantAttribute>()
                         .HasKey(c => c.VariantAttributeCode);
            modelBuilder.Entity<Basket>()
                         .HasKey(c => c.BuyerId);
            modelBuilder.Entity<BasketItem>()
                         .HasKey(c => c.ProductCode);
            modelBuilder.Entity<BasketItem>()
                        .HasKey(bi => new { bi.ProductCode, bi.BuyerID });
            modelBuilder.Entity<Order>()
                         .HasKey(c => c.Id); 
            modelBuilder.Entity<OrderItem>()
                        .HasKey(oi => new { oi.ProductCode, oi.OrderId });
            modelBuilder.Entity<Address>()
                        .HasKey(i=> i.Id);
            modelBuilder.Entity<UserAudit>()
                        .HasKey(i=> i.UserId);
            modelBuilder.Entity<PaymentLog>()
                        .HasKey(i=> i.ConversationId);


            modelBuilder.Entity<RelatedProduct>()
            .HasKey(rp => new { rp.ProductCode, rp.RelatedProductCode });
            modelBuilder.Entity<Bundle>()
            .HasKey(b => new { b.BundleCode, b.ProductCode });
            // Fluent API Configuration
            modelBuilder.Entity<Subscriber>(entity =>
            {
                entity.Property(e => e.EmailAddress)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.SubscribedOn)
                    .IsRequired();
            });
            modelBuilder.Entity<Catalog>(entity =>
            {
                entity.Property(e => e.CatalogName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.CatalogDescription)
                    .IsRequired();
            });
            modelBuilder.Entity<Product>()
              .HasOne(c => c.Catalog)
              .WithMany(p => p.Products)
              .HasForeignKey(c => c.CatalogId);
            modelBuilder.Entity<ProductVariant>()
                     .HasOne(pv => pv.Product)           
                     .WithMany(p => p.ProductVariants)          
                     .HasForeignKey(pv => pv.BaseProduct)
                     .HasPrincipalKey(p => p.ProductCode);

            modelBuilder.Entity<BasketItem>()
                     .HasOne(p => p.ProductVariant)           
                     .WithMany(p => p.BasketItems)          
                     .HasForeignKey(p => p.ProductCode)
                     .HasPrincipalKey(p => p.ProductCode);
            modelBuilder.Entity<OrderItem>()
                     .HasOne(p => p.ProductVariant)
                     .WithMany(p => p.OrderItems)
                     .HasForeignKey(p => p.ProductCode);

            modelBuilder.Entity<Discount>()
            .HasOne(d => d.Product)
            .WithMany(p => p.Discounts)
            .HasForeignKey(d => d.ProductCode)
            .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Discount>()
            .HasOne(d => d.ProductVariant)
            .WithMany(p => p.Discounts)
            .HasForeignKey(d => d.ProductCode)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RelatedProduct>()
             .HasOne(rp => rp.Product)
             .WithMany(p => p.RelatedProducts)
             .HasForeignKey(rp => rp.ProductCode)
             .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RelatedProduct>()
         .HasOne(rp => rp.Product)
         .WithMany(p => p.RelatedProducts)
         .HasForeignKey(rp => rp.ProductCode)
         .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RelatedProduct>()
                .HasOne(rp => rp.RelatedProducts)
                .WithMany()
                .HasForeignKey(rp => rp.RelatedProductCode)
                .OnDelete(DeleteBehavior.NoAction);


            // Variant Primary Key
            modelBuilder.Entity<Variant>()
                .HasKey(v => v.VariantCode);

            // Variant Attributes Relationship
            modelBuilder.Entity<VariantAttribute>()
                .HasKey(va => va.VariantAttributeCode);

            modelBuilder.Entity<VariantAttribute>()
                .HasOne(va => va.Variant)
                .WithMany(v => v.VariantAttributes)
                .HasForeignKey(va => va.VariantCode)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BasketItem>()
                .HasOne(b => b.Basket)
                .WithMany(bi => bi.Items)
                .HasForeignKey(bi => bi.BuyerID)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Order>()
                        .HasOne(o => o.ShipToAddress)
                        .WithMany(a => a.Orders)
                        .HasForeignKey(o => o.ShipToAddressId)
                        .OnDelete(DeleteBehavior.Restrict); // Optional, depending on your cascade behavior preference

            // Configure Order -> BillingAddress (One-to-One or many-to-one depending on your logic)
            modelBuilder.Entity<Order>()
                        .HasOne(o => o.BillingAddress)
                        .WithMany() // If BillingAddress is not used for multiple orders, use .WithOne()
                        .HasForeignKey(o => o.BillingAddressId)
                        .OnDelete(DeleteBehavior.Restrict); // Optional

            modelBuilder.Entity<PaymentLog>()
                .HasOne(O => O.Order)
                .WithOne(p => p.PaymentLog);

        }
    }
}
