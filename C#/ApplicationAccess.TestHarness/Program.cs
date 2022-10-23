using System;

namespace ApplicationAccess.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            var testDataElementStorer = new DataElementStorer<String, String, Screen, Access>();

            testDataElementStorer.AddUser("User1");
            testDataElementStorer.AddEntityType("Customers");
            testDataElementStorer.AddEntityType("Products");
            testDataElementStorer.AddEntity("Customers", "Client1");
            testDataElementStorer.AddEntity("Customers", "Client2");
            testDataElementStorer.AddEntity("Products", "Boxes");
            testDataElementStorer.AddEntity("Products", "Forks");
            testDataElementStorer.AddEntity("Products", "Spoons");
            testDataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping("User2", Screen.Screen1, Access.Delete);
            testDataElementStorer.AddUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.AddUserToEntityMapping("User3", "Customers", "Client1");
            testDataElementStorer.AddUserToEntityMapping("User4", "Customers", "Client3");
            testDataElementStorer.AddUserToEntityMapping("User5", "Systems", "Order");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 5
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 2
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 3
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 7
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 4
            testDataElementStorer.RemoveEntityType("Systems");
            testDataElementStorer.RemoveUser("User5");
            testDataElementStorer.RemoveUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.RemoveUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.RemoveEntity("Customers", "Client1");
            testDataElementStorer.RemoveUser("User1");
            testDataElementStorer.RemoveUser("User2");
            testDataElementStorer.RemoveUser("User3");
            testDataElementStorer.RemoveUser("User4");
            testDataElementStorer.RemoveEntityType("Customers");
            testDataElementStorer.RemoveEntityType("Products");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 0
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 0
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 0
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 0
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 0

            testDataElementStorer.RemoveEntityType("Systems");
            testDataElementStorer.RemoveUser("User5");
            testDataElementStorer.RemoveUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.RemoveUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.RemoveEntity("Customers", "Client1");
            testDataElementStorer.RemoveUser("User1");
            testDataElementStorer.RemoveUser("User2");
            testDataElementStorer.RemoveUser("User3");
            testDataElementStorer.RemoveUser("User4");
            testDataElementStorer.RemoveEntityType("Customers");
            testDataElementStorer.RemoveEntityType("Products");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 0
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 0
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 0
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 0
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 0

        }

        protected enum Screen
        {
            Screen1,
            Screen2,
            Screen3,
            Screen4,
            Screen5,
            Screen6,
            Screen7,
            Screen8,
            Screen9
        }

        protected enum Access
        {
            Read, 
            Create, 
            Update, 
            Delete
        }
    }
}
