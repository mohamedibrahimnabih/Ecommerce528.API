namespace ECommerce528
{
    public static class DependencyInjection
    {
        public static void Configure(this IServiceCollection services)
        {
            // Repos
            services.AddScoped<IRepository<Category>, Repository<Category>>();
            services.AddScoped<IRepository<Category>, Repository<Category>>();
            services.AddScoped<IRepository<Brand>, Repository<Brand>>();
            services.AddScoped<IRepository<Product>, Repository<Product>>();
            services.AddScoped<IRepository<ApplicationUserOTP>, Repository<ApplicationUserOTP>>();
            services.AddScoped<IRepository<Cart>, Repository<Cart>>();
            services.AddScoped<IRepository<FavoriteItem>, Repository<FavoriteItem>>();
            services.AddScoped<IRepository<ProductPromotion>, Repository<ProductPromotion>>();
            services.AddScoped<IRepository<PromotionUserUsage>, Repository<PromotionUserUsage>>();
            services.AddScoped<IRepository<ProductSubImg>, Repository<ProductSubImg>>();
            services.AddScoped<IProductSubImgRepository, ProductSubImgRepository>();
            services.AddScoped<IRepository<Order>, Repository<Order>>();
            services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();
            services.AddScoped<IRepository<Review>, Repository<Review>>();

            // Services
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IUserService, UserService>();

            // Others
            services.AddScoped<IDbInitializer, DbInitializer>();
        }
    }
}
