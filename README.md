ApplicationAccess
-----------------

## Overview

ApplicationAccess contains classes for managing user authorization and access to an application.  It allows client-configurable application components and levels of access, and supports mapping of combinations of application components and access levels to users and groups of users.  It focuses on speed and efficiency when checking whether a user has access to a given component, and includes thorough and informative error handling.


## Motivation and Background

If you're a developer of GUI applications and/or APIs requiring user authorization, you will have either had to maintain (or maybe build from scratch) system components for user permissions or authorization.  Typically these types of components are not amongst the most interesting thing you can work on in software development... often just implementing simple mapping via relevant data structures, and requiring significant effort expended on basic (but important) things like exception handling.  After recently having to implement yet another set of components for authorization (for the 3rd or 4th time in my career), I decided to build a set of reusable components for user authorization, with the goal of subsequently never having to write authorization components again.  By making use of generic types for users, groups, application components, and levels of access, and allowing user definable 'entities' to additionally be mapped to users and groups, my hope is that ApplicationAccess is flexible enough to support the authorization requirements of ~90% of real-world applications.  I've spent the time on implementing the aforementioned mapping and exception handling so that I (and you) don't have to do it again :smiley:.


## Data Elements and Mappings

The central class which manages authorization is the [AccessManager](C#/ApplicationAccess/AccessManager.cs).  It implements 3 main types of data mapping...

**Users and Groups**
Users and groups are stored in a directed graph, where users are stored as leaf vertices/nodes (or 'start' vertices in terms of directed traversal), and groups are stored as non-leaf vertices.  Storing user to group mapping in a graph structure has a number of benefits...
* Users can be members of multiple groups
* Groups can be members of other groups
* There's no limit to the depth of user &gt; group &gt; group hierarchy that can be mapped

**Application Components and Access Levels**
Application components represent (as the name suggests) components within an application.  For a GUI application, each screen would likely be represented by an application component... in an API application, an application component could correspond to a single API endpoint.  Access levels represent typical levels of access like create, read, update, etc... in an API application they could correspond to REST verbs (GET, PUT, POST, etc...).

**Entities**
Entities have a generic name, and were designed to be a generic way to represent other types of entities/elements which need permissions mapped to them, but don't fit into the category of an application component.  Whilst application components are designed to represent elements like screens or API endpoints in an application, entities could represent more domain-specific elements like clients in a CRM system or products lines in an order management system.  Whereas application components are based on type parameters, entities are strings... this has a benefit in that new entities can be created dynamically during runtime.  In addition to checking whether a given user (or any of its parent groups) are mapped to an entity, the AccessManager class can return a list of all entities a user is mapped to... this is useful in situation where you want filter a set of data before making it available to the current user.  For example in the aforementioned CRM application scenario, a screen showing a list of recent client interactions could be filtered to only show interactions for clients that the current user has access to.  Entities are categorized/grouped into types for clearer organization.


## Example

### Use Case

To demonstrate ApplicationAccess, we'll look at how the AccessManager class could manage authorization/permissions for a software application supporting a small company.  The company is a fictitious supplier of printing and weaving machines, which has the following organizational structure...

![Company Structure](http://alastairwyse.net/applicationaccess/images/company-structure.png)

The software application contains 5 screens...

<table>
  <tr>
    <td><b>Screen</b></td>
    <td><b>Purpose</b></td>
  </tr>
  <tr>
    <td valign="top">Order</td>
    <td>Allows entering a client order for a printing or weaving machine</td>
  </tr>
  <tr>
    <td valign="top">OrderSummary</td>
    <td>Shows a summary of all client orders</td>
  </tr>
  <tr>
    <td valign="top">ProductSetup</td>
    <td>Allows setup of a new printing or weaving machine (name, manufacturer, price, etc...)</td>
  </tr>
  <tr>
    <td valign="top">SystemSettings</td>
    <td>System settings of this application.</td>
  </tr>
  <tr>
    <td valign="top">ClientInteractions</td>
    <td>Allows creating a viewing of records which list interactions with clients (via phone, email, etc...)</td>
  </tr>
</table>

### AccessManager Setup

First we need types to represent users, groups, application components, and access levels.  In this case for users and groups we'll use simple strings... the staff member's email address to represent the user and a string containing the group name for groups.  For application components and access levels we'll use the following enums...

```c#
public enum Screen
{
    Order, 
    OrderSummary, 
    ProductSetup, 
    SystemSettings, 
    ClientInteractions
}

public enum AccessLevel
{
    View, 
    Modify
}
```

We create the access manager class and add all the company staff as below...

```c#
var accessManager = new AccessManager<String, String, Screen, AccessLevel>();

// Add the staff members
accessManager.AddUser("Livia.Bowe@printweave.biz");
accessManager.AddUser("Arjan.Hartman@printweave.biz");
accessManager.AddUser("Cleo.Short@printweave.biz");
accessManager.AddUser("Mae.Mellor@printweave.biz");
accessManager.AddUser("Frankie.Koch@printweave.biz");
accessManager.AddUser("Deborah.Moss@printweave.biz");
accessManager.AddUser("Kishan.Buchanan@printweave.biz");
accessManager.AddUser("Seb.Sutton@printweave.biz");
accessManager.AddUser("Bo.Wagner@printweave.biz");
accessManager.AddUser("Tye.Knights@printweave.biz");
```

The teams/groups within the company are setup as follows.  We're going to create a separate group for the managers within the sales teams, and also an 'AllStaff' group.  Having a group containing all staff members means we can avoid duplicating a lot of permissions across users (since the required permissions can just be mapped to the 'AllStaff' group)...

```c#
// Add the teams/groups within the company
accessManager.AddGroup("Sales");
accessManager.AddGroup("SalesManagers");
accessManager.AddGroup("Managers");
accessManager.AddGroup("IT");
accessManager.AddGroup("CustomerService");
accessManager.AddGroup("AllStaff");
```

Now setup user to group mappings (to indicate which teams staff are members of), and also group to group mapping...

```c#
// Setup the company structure - user to team mapping
accessManager.AddUserToGroupMapping("Livia.Bowe@printweave.biz", "Sales");
accessManager.AddUserToGroupMapping("Arjan.Hartman@printweave.biz", "Sales");
accessManager.AddUserToGroupMapping("Frankie.Koch@printweave.biz", "Sales");
accessManager.AddUserToGroupMapping("Cleo.Short@printweave.biz", "SalesManagers");
accessManager.AddUserToGroupMapping("Mae.Mellor@printweave.biz", "SalesManagers");
accessManager.AddUserToGroupMapping("Deborah.Moss@printweave.biz", "CustomerService");
accessManager.AddUserToGroupMapping("Kishan.Buchanan@printweave.biz", "CustomerService");
accessManager.AddUserToGroupMapping("Seb.Sutton@printweave.biz", "IT");
accessManager.AddUserToGroupMapping("Bo.Wagner@printweave.biz", "Managers");
accessManager.AddUserToGroupMapping("Tye.Knights@printweave.biz", "Managers");

// Setup the team/group mappings
accessManager.AddGroupToGroupMapping("SalesManagers", "Sales");
accessManager.AddGroupToGroupMapping("Sales", "AllStaff");
accessManager.AddGroupToGroupMapping("Managers", "AllStaff");
accessManager.AddGroupToGroupMapping("IT", "AllStaff");
accessManager.AddGroupToGroupMapping("CustomerService", "AllStaff");
```

... the directed graph stored internally in the AccessManager now looks like this...

![AccessManager Directed Graph Representation](http://alastairwyse.net/applicationaccess/images/graph-representation.png)

Next we setup mappings to denote teams/groups access to screens in the software.  Staff in the various teams have 'view' and 'modify' access to the screens relevant to their function within the company.  In addition, the following additional permissions are assigned...

* Sales managers are able to modify the 'ProductSetup' screen in order to input details of new and upcoming products
* IT staff are able to configure the system via modifications to the 'SystemSettings' screen

```c#
// Setup access to the system/application
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("AllStaff", Screen.OrderSummary, AccessLevel.View);
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("AllStaff", Screen.ClientInteractions, AccessLevel.View);
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Sales", Screen.Order, AccessLevel.Modify);
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("SalesManagers", Screen.ProductSetup, AccessLevel.Modify);
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("CustomerService", Screen.ClientInteractions, AccessLevel.Modify);
accessManager.AddGroupToApplicationComponentAndAccessLevelMapping("IT", Screen.SystemSettings, AccessLevel.Modify);
```

Note that in this case we setup mappings at the group level, but the same mappings can be applied directly to individual users.

The final step is to setup entities and again map them to users/groups.  In this case we'll setup two types of entities...one to denote different clients of the company, and another to denote different product lines (being printing machines and weaving machines).

```c#
// Setup additional entities
accessManager.AddEntityType("Clients");
accessManager.AddEntity("Clients", "CompanyA");
accessManager.AddEntity("Clients", "CompanyB");
accessManager.AddEntity("Clients", "CompanyC");
accessManager.AddEntityType("Products");
accessManager.AddEntity("Products", "PrintingMachines");
accessManager.AddEntity("Products", "WeavingMachines");
```

The 'Products' entities would be used on the 'Order' and 'OrderSummary' to filter lists of orders and drop-down lists containing the products... e.g. so that the members of the printing machines sales team could only view and create orders for printing machines.  The 2 members of the customer service team each deal exclusively with specific client companies, so entities can again be used to filter the relevant systems screens to support this (specifically the 'ClientInteractions' screen could be filtered to only show and allow creation of customer interaction records for the clients mapped to the current system user).  These user to entity mappings are setup as follows...

```c#
// Setup user to entity mappings
accessManager.AddUserToEntityMapping("Livia.Bowe@printweave.biz", "Products", "PrintingMachines");
accessManager.AddUserToEntityMapping("Arjan.Hartman@printweave.biz", "Products", "PrintingMachines");
accessManager.AddUserToEntityMapping("Cleo.Short@printweave.biz", "Products", "PrintingMachines");
accessManager.AddUserToEntityMapping("Frankie.Koch@printweave.biz", "Products", "WeavingMachines");
accessManager.AddUserToEntityMapping("Mae.Mellor@printweave.biz", "Products", "WeavingMachines");
accessManager.AddUserToEntityMapping("Deborah.Moss@printweave.biz", "Clients", "CompanyA");
accessManager.AddUserToEntityMapping("Deborah.Moss@printweave.biz", "Clients", "CompanyB");
accessManager.AddUserToEntityMapping("Kishan.Buchanan@printweave.biz", "Clients", "CompanyA");
accessManager.AddUserToEntityMapping("Kishan.Buchanan@printweave.biz", "Clients", "CompanyB");
accessManager.AddUserToEntityMapping("Kishan.Buchanan@printweave.biz", "Clients", "CompanyC");
```
We've applied the above mappings to individual users, but they can equally be applied to groups.

### AccessManager Use at Runtime

The 'HasAccess' methods are used to check whether a given user can access an application component or entity.  For example the following call would return false as Livia Bowe is not a member of the 'SalesManagers' group and hence cannot modify the 'ProductSetup' screen...

```c#
accessManager.HasAccessToApplicationComponent("Livia.Bowe@printweave.biz", Screen.ProductSetup, AccessLevel.Modify);  // returns false
```

Since Mae Mellor is a member of 'SalesManagers', the same call returns true for them...

```c#
accessManager.HasAccessToApplicationComponent("Mae.Mellor@printweave.biz", Screen.ProductSetup, AccessLevel.Modify);  // returns true
```

As a member of the Weaving Machine Sales team, Frankie Koch has access to the 'WeavingMachines' entity...

```c#
accessManager.HasAccessToEntity("Frankie.Koch@printweave.biz", "Products", "WeavingMachines");  // returns true
```

...Arjan Hartman, as member of the Printing Machine Sales team does not...

```c#
accessManager.HasAccessToEntity("Arjan.Hartman@printweave.biz", "Products", "WeavingMachines");  // returns false
```

The GetUserToEntityMappings() and GetGroupToEntityMappings() can be used to retrieve all the entities mapped to a specified user or group.  This is useful for filtering lists and drop-down menus in a UI, to show only items available to the current user.  For example, the ClientInteractions screen could be filtered to only show the clients returned below (assuming Kishan Buchanan had accessed the screen)...

```c#
var viewableClients = new HashSet<String>(accessManager.GetUserToEntityMappings("Kishan.Buchanan@printweave.biz", "Clients"));
// 'viewableClients' contains...
//   "CompanyA"
//   "CompanyB"
//   "CompanyC"
```

It's assumed the above calls to the 'HasAccess' and 'GetEntityMappings' methods would occur towards the start of a method implementing either access to a UI component, or an API endpoint (e.g. Controller class in an ASP.NET application).

### Serialization

The [AccessManagerJsonSerializer](C#/ApplicationAccess.Serialization/AccessManagerJsonSerializer.cs) class performs serialization of an AccessManager to and from JSON.  The Serialize() and Deserialize() methods require implementations of interface IUniqueStringifier&lt;T&gt; for the generic types representing users, groups, application components, and access levels.  Classes 'StringUniqueStringifier' and 'EnumUniqueStringifier&lt;T&gt;' are included to handle string and enum generic types... other  generic types would require a custom implementation of IUniqueStringifier&lt;T&gt;.

Serialization of the above access manager would be performed as follows...

```c#
var mySerializer = new AccessManagerJsonSerializer();
JObject serializedAccessManager = mySerializer.Serialize<String, String, Screen, AccessLevel>
(
    accessManager, 
    new StringUniqueStringifier(),
    new StringUniqueStringifier(), 
    new EnumUniqueStringifier<Screen>(),
    new EnumUniqueStringifier<AccessLevel>()
);

Similarly deserialization is performed like this...

AccessManager<String, String, Screen, AccessLevel> deserializedAccessManager = mySerializer.Deserialize<String, String, Screen, AccessLevel>
(
    serializedAccessManager,
    new StringUniqueStringifier(),
    new StringUniqueStringifier(),
    new EnumUniqueStringifier<Screen>(),
    new EnumUniqueStringifier<AccessLevel>()
);
```


## Additional Notes

### Setup Choices

The available data elements in the AccessManager are designed to be flexible enough to accommodate multiple different mappings to the real-world components and entities they represent.  In the above example we created a single group containing all members of the sales team, and then used entities to mapped to individual sales team members to split printing machine sales and weaving machine sales.  However, another approach would be to use a group hierarchy to represent this... i.e. make the individual staff members of separate 'WeavingMachineSales' and 'PrintingMachineSales' groups, and then have both these groups mapped to a parent 'Sales' group.

### Generic Type Choices

The types used for generics TUser, TGroup, TComponent, and TAccess must implement the relevant methods to allow them to be stored in HashSets and used as keys in Dictionary classes (in .NET implementing IEquatable&lt;T&gt; and overriding GetHashCode()).  Typically in C# I would use generic type constraints to enforce this at compile-time, however doing so prevents the use of enums as they don't support IEquatable&lt;T&gt; by default in .NET... since enums would be a common choice for TComponent and TAccess, I decided to remove the type constraints.

Using enums or custom classes for TComponent and TAccess is recommended as it results in more compile-time checking, however strings could be used if the components or access level values need to change at runtime (e.g. in a UI application where users are able to dynamically generate their own screens).

For TUser and TGroup anything which uniquely identifies a user or a group is appropriate.  Since users and groups could be added any removed dynamically at runtime, strings are likely a better choice than enums, however a class like [UserPrincipal](https://docs.microsoft.com/en-us/dotnet/api/system.directoryservices.accountmanagement.userprincipal?view=dotnet-plat-ext-5.0) (or custom model class) could be used for a more sophisticated implementation in Windows Active Directory environments (again, provided it implements IEquatable&lt;T&gt; etc... as described above).

### HasAccess*() Method Implementation

The user to group mapping in the AccessManager class is implemented as a directed graph.  When the HasAccess*() methods are called, a depth-first traversal of the graph is performed starting at the specified user.  The traversal code will not re-traverse sections of the graph, in the case that a parent group has multiple paths to it.  Also in the case of a true result, traversal will stop as soon as a matching permission is found.  The cost of traversal scales roughly according to the average number of parent groups each user has.

### Remove*() Method Performance

Calling methods RemoveEntity() or RemoveEntityType() will iterate all user to entity and group to entity mappings in the AccessManager class, to ensure that any mappings to the entity or entity type are cleaned-up/removed.  Similarly calling the RemoveGroup() method will traverse the whole user/group graph to ensure that any mappings to the removed group are also removed.  Whilst there's a reasonable performance cost for calling any of these methods, at the same time it's assumed that they will be called relatively infrequently (as compared to HasAccess*() or GetUserToEntityMappings()).  If these Remove*() methods do need to be called frequently, the AccessManager could be subclassed or enhanced to create bi-directional mappings between users, groups, and entities, and in the user/group graph.


## Future Enhancements

* Python version
* Database persistence
* Include JSON schema check in deserialization code

## Release History

<table>
    <tr>
        <td><b>Version</b></td>
        <td><b>Changes</b></td>
    </tr>
    <tr>
        <td valign="top">1.0.0</td>
        <td>
            First Release.
        </td>
    </tr>
</table>

