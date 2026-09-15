using Microsoft.EntityFrameworkCore;
using FarmFlow.Web.Models;

namespace FarmFlow.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Farm> Farms { get; set; }
    public DbSet<Crop> Crops { get; set; }
    public DbSet<ProductionBatch> ProductionBatches { get; set; }

    // Inventory
    public DbSet<Category> Categories { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }

    // ✅ NEW: Purchasing
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    // Sales & CRM
public DbSet<Customer> Customers { get; set; }
public DbSet<SalesOrder> SalesOrders { get; set; }
public DbSet<SalesOrderItem> SalesOrderItems { get; set; }
public DbSet<CrmInteraction> CrmInteractions { get; set; }
public DbSet<Quotation> Quotations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================
        // UNIQUE CONSTRAINTS
        // ============================================
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        modelBuilder.Entity<Farm>().HasIndex(f => f.FarmCode).IsUnique().HasDatabaseName("IX_Farm_Code");
        modelBuilder.Entity<Crop>().HasIndex(c => c.CropCode).IsUnique().HasDatabaseName("IX_Crop_Code");
        modelBuilder.Entity<ProductionBatch>().HasIndex(p => p.BatchCode).IsUnique().HasDatabaseName("IX_Batch_Code");
        modelBuilder.Entity<InventoryItem>().HasIndex(i => i.ItemCode).IsUnique().HasDatabaseName("IX_Item_Code");
        modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();

        // ✅ NEW: Purchasing unique constraints
        modelBuilder.Entity<Supplier>().HasIndex(s => s.SupplierCode).IsUnique().HasDatabaseName("IX_Supplier_Code");
        modelBuilder.Entity<PurchaseOrder>().HasIndex(p => p.PONumber).IsUnique().HasDatabaseName("IX_PO_Number");

        // ============================================
        // INVENTORY RELATIONSHIPS (Restrict)
        // ============================================
        modelBuilder.Entity<StockTransfer>()
            .HasOne(s => s.FromLocation)
            .WithMany()
            .HasForeignKey(s => s.FromLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(s => s.ToLocation)
            .WithMany()
            .HasForeignKey(s => s.ToLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryItem>()
            .HasOne(i => i.Category)
            .WithMany(c => c.InventoryItems)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.InventoryItem)
            .WithMany(i => i.InventoryTransactions)
            .HasForeignKey(t => t.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.PerformedByUser)
            .WithMany()
            .HasForeignKey(t => t.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(s => s.InventoryItem)
            .WithMany(i => i.StockTransfers)
            .HasForeignKey(s => s.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransfer>()
            .HasOne(s => s.PerformedByUser)
            .WithMany()
            .HasForeignKey(s => s.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================
        // ✅ NEW: PURCHASING RELATIONSHIPS (Restrict)
        // ============================================
        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.RequestedByUser)
            .WithMany()
            .HasForeignKey(p => p.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.ApprovedByUser)
            .WithMany()
            .HasForeignKey(p => p.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany(p => p.PurchaseOrderItems)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrderItem>()
            .HasOne(i => i.InventoryItem)
            .WithMany()
            .HasForeignKey(i => i.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

            // Unique constraints
modelBuilder.Entity<Customer>().HasIndex(c => c.CustomerCode).IsUnique().HasDatabaseName("IX_Customer_Code");
modelBuilder.Entity<SalesOrder>().HasIndex(s => s.SONumber).IsUnique().HasDatabaseName("IX_SO_Number");
modelBuilder.Entity<Quotation>().HasIndex(q => q.QuotationNumber).IsUnique().HasDatabaseName("IX_Quotation_Number");

// Relationships (Restrict to avoid cascade issues)
modelBuilder.Entity<Customer>()
    .HasOne(c => c.AssignedSalesRep)
    .WithMany()
    .HasForeignKey(c => c.AssignedSalesRepId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<SalesOrder>()
    .HasOne(s => s.Customer)
    .WithMany(c => c.SalesOrders)
    .HasForeignKey(s => s.CustomerId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<SalesOrder>()
    .HasOne(s => s.SalesRep)
    .WithMany()
    .HasForeignKey(s => s.SalesRepId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<SalesOrderItem>()
    .HasOne(i => i.SalesOrder)
    .WithMany(s => s.SalesOrderItems)
    .HasForeignKey(i => i.SalesOrderId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<SalesOrderItem>()
    .HasOne(i => i.InventoryItem)
    .WithMany()
    .HasForeignKey(i => i.InventoryItemId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<CrmInteraction>()
    .HasOne(i => i.Customer)
    .WithMany(c => c.CrmInteractions)
    .HasForeignKey(i => i.CustomerId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<CrmInteraction>()
    .HasOne(i => i.SalesRep)
    .WithMany()
    .HasForeignKey(i => i.SalesRepId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Quotation>()
    .HasOne(q => q.Customer)
    .WithMany(c => c.Quotations)
    .HasForeignKey(q => q.CustomerId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<Quotation>()
    .HasOne(q => q.SalesRep)
    .WithMany()
    .HasForeignKey(q => q.SalesRepId)
    .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Customer>().HasData(
    new Customer { Id = 1, CustomerCode = "CUST-001", CustomerType = "Wholesaler", CompanyName = "Green Harvest Inc.", FirstName = "Maria", LastName = "Santos", Email = "maria@greenharvest.com", PhoneNumber = "09181234567", Address = "123 Market St., Manila", PaymentTerms = "Net 30", DiscountTier = "Gold", IsActive = true, CreatedDateTime = new DateTime(2024, 1, 1) },
    new Customer { Id = 2, CustomerCode = "CUST-002", CustomerType = "Retailer", CompanyName = "Fresh Mart Philippines", FirstName = "Juan", LastName = "Reyes", Email = "juan@freshmart.ph", PhoneNumber = "09191234567", Address = "456 Retail Ave., Quezon City", PaymentTerms = "Net 15", DiscountTier = "Silver", IsActive = true, CreatedDateTime = new DateTime(2024, 1, 1) },
    new Customer { Id = 3, CustomerCode = "CUST-003", CustomerType = "Exporter", CompanyName = "Asia Agri Exports", FirstName = "Pedro", LastName = "Cruz", Email = "pedro@asiaagri.com", PhoneNumber = "09201234567", Address = "789 Export Blvd., Cebu", PaymentTerms = "Net 60", DiscountTier = "Gold", IsActive = true, CreatedDateTime = new DateTime(2024, 1, 1) }
);

        // ============================================
        // SEED ROLES
        // ============================================
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Administrator", Description = "Full system access", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Role { Id = 2, Name = "Production_Manager", Description = "Manage production and crops", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Role { Id = 3, Name = "Warehouse_Keeper", Description = "Manage inventory", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Role { Id = 4, Name = "Purchaser", Description = "Manage purchasing", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Role { Id = 5, Name = "Sales_Rep", Description = "Manage sales and customers", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Role { Id = 6, Name = "Accountant", Description = "Manage finances", CreatedDateTime = new DateTime(2024, 1, 1) }
        );

        // ============================================
        // SEED CATEGORIES
        // ============================================
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Seeds", Description = "Planting seeds and seedlings", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Category { Id = 2, Name = "Fertilizers", Description = "Soil nutrients and fertilizers", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Category { Id = 3, Name = "Pesticides", Description = "Pest and disease control", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Category { Id = 4, Name = "Harvested Crops", Description = "Produce ready for sale", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Category { Id = 5, Name = "Tools", Description = "Farm tools and equipment", CreatedDateTime = new DateTime(2024, 1, 1) },
            new Category { Id = 6, Name = "Fuel", Description = "Diesel, gasoline, and lubricants", CreatedDateTime = new DateTime(2024, 1, 1) }
        );

        // ============================================
        // SEED WAREHOUSE LOCATIONS
        // ============================================
        modelBuilder.Entity<WarehouseLocation>().HasData(
            new WarehouseLocation { Id = 1, LocationName = "Main Warehouse", LocationType = "Dry Storage", Capacity = 1000, CreatedDateTime = new DateTime(2024, 1, 1) },
            new WarehouseLocation { Id = 2, LocationName = "Cold Storage Room A", LocationType = "Cold Storage", Capacity = 500, CreatedDateTime = new DateTime(2024, 1, 1) },
            new WarehouseLocation { Id = 3, LocationName = "Fertilizer Storage", LocationType = "Dry Storage", Capacity = 800, CreatedDateTime = new DateTime(2024, 1, 1) }
        );

        // ============================================
        // ✅ NEW: SEED SUPPLIERS
        // ============================================
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier
            {
                Id = 1,
                SupplierCode = "SUP-001",
                CompanyName = "AgriSupply Co.",
                ContactPerson = "Juan Dela Cruz",
                ContactEmail = "juan@agrisupply.com",
                ContactPhone = "09171234567",
                Address = "123 Agriculture St., Manila",
                PaymentTerms = "Net 30",
                CreditLimit = 50000,
                PerformanceRating = 4.5m,
                IsActive = true,
                CreatedDateTime = new DateTime(2024, 1, 1)
            },
            new Supplier
            {
                Id = 2,
                SupplierCode = "SUP-002",
                CompanyName = "Green Farm Inputs",
                ContactPerson = "Maria Santos",
                ContactEmail = "maria@greenfarm.com",
                ContactPhone = "09181234567",
                Address = "456 Farming Ave., Laguna",
                PaymentTerms = "Net 15",
                CreditLimit = 30000,
                PerformanceRating = 4.8m,
                IsActive = true,
                CreatedDateTime = new DateTime(2024, 1, 1)
            },
            new Supplier
            {
                Id = 3,
                SupplierCode = "SUP-003",
                CompanyName = "Fertilizer Depot Inc.",
                ContactPerson = "Pedro Reyes",
                ContactEmail = "pedro@fertDepot.com",
                ContactPhone = "09191234567",
                Address = "789 Agro Rd., Cavite",
                PaymentTerms = "COD",
                CreditLimit = 20000,
                PerformanceRating = 4.2m,
                IsActive = true,
                CreatedDateTime = new DateTime(2024, 1, 1)
            }
        );
    }
}