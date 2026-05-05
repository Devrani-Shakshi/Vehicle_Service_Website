namespace ServicePlatform.Helpers;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string ServiceProvider = "ServiceProvider";
        public const string Shopkeeper = "Shopkeeper";
    }

    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string UserOnly = "UserOnly";
        public const string ServiceProviderOnly = "ServiceProviderOnly";
        public const string ShopkeeperOnly = "ShopkeeperOnly";
    }

    public static readonly string[] AllStates = new[]
    {
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar",
        "Chhattisgarh", "Goa", "Gujarat", "Haryana",
        "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala",
        "Madhya Pradesh", "Maharashtra", "Manipur", "Meghalaya",
        "Mizoram", "Nagaland", "Odisha", "Punjab",
        "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana",
        "Tripura", "Uttar Pradesh", "Uttarakhand", "West Bengal",
        "Delhi", "Chandigarh", "Puducherry", "Jammu and Kashmir",
        "Ladakh", "Lakshadweep", "Dadra and Nagar Haveli",
        "Andaman and Nicobar Islands"
    };

    public static readonly string[] ProductCategories = new[]
    {
        "Battery & Power", "Charging Accessories", "Spare Parts", 
        "Car Care", "Tools & Accessories", "Lighting & Electrical", 
        "Maintenance Kits", "Other Parts"
    };
}
