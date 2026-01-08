# Application Access

A containerized REST-based application for authorization and permission/privilege management.

## Features

* CQRS - Read (query) and write (event) operations independently scalable
* Extensive use of generic classes and methods to aid code reusability
* Recovery features - Secondary persistence, database and network transient error recovery, tripswitch, deadlock retries
* Code reuse - Core AccessManager classes used for single node, multi-reader, and distributed/sharded deployment profiles, plus data validation.
* Consistent hashing used for sharding
* Event sourcing - simplifies CQRS implementation and redistribution
* Temporal data model - Data can be queried at and historic point in time
* Concurrency management - lock manager to prevent deadlocks, arbitary derivated functionality can be run under lock context
* Extensive metrics and observabililty
* Randomized load and performance testing
* Exception propagation and rehydration between distributed components
* General separation of concerns - Implementing to abstract interfaces, multiple database implementations (commonality between SQL implementations), selectable REST or gRPC protocol for communication between distributed components
* Dynamic redistribution / re-sharding - Distributed deployment profile can be changed dynamically whilst continuing to respond to operations (mention DistOpRouter)
* Mapping of exceptions to REST and gRPC status codes done via middleware/interceptors.  Avoids repeated status code mapping within controller methods.
* DistOpCoord is stateless and provides limitless scaling 


## REST Endpoints

| URL Path | Method | Description |
| -------- | ------ | ----------- |
| /api/v1/entityTypes/{entityType} | POST | Adds an entity type. |
| /api/v1/entityTypes/{entityType} | DELETE | Removes an entity type. |
| /api/v1/entityTypes/{entityType} | GET | Returns the specified entity type if it exists. |
| /api/v1/entityTypes/{entityType}/entities/{entity} | POST | Adds an entity. |
| /api/v1/entityTypes/{entityType}/entities/{entity} | DELETE | Removes an entity. |
| /api/v1/entityTypes/{entityType}/entities/{entity} | GET | Returns the specified entity if it exists. |
| /api/v1/entityTypes | GET | Returns all entity types. |
| /api/v1/entityTypes/{entityType}/entities | GET | Returns all entities of the specified type. |
| /api/v1/groups/{group} | POST | Adds a group. |
| /api/v1/groups/{group} | DELETE | Removes a group. |
| /api/v1/groups/{group} | GET | Returns the specified group if it exists. |
| /api/v1/groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | POST | Adds a mapping between the specified group, application component, and level of access to that component. |
| /api/v1/groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | DELETE | Removes a mapping between the specified group, application component, and level of access to that component. |
| /api/v1/groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity} | POST | Adds a mapping between the specified group, and entity. |
| /api/v1/groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity} | DELETE | Removes a mapping between the specified group, and entity. |
| /api/v1/groups | GET | Returns all groups. |
| /api/v1/groupToApplicationComponentAndAccessLevelMappings/group/{group} | GET | Gets the application component and access level pairs that the specified group is mapped to. |
| /api/v1/groupToApplicationComponentAndAccessLevelMappings/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | GET | Gets the groups that are mapped to the specified application component and access level pair. |
| /api/v1/groupToEntityMappings/group/{group} | GET | Gets the entities that the specified group is mapped to. |
| /api/v1/groupToEntityMappings/group/{group}/entityType/{entityType} | GET | Gets the entities of a given type that the specified group is mapped to. |
| /api/v1/groupToEntityMappings/entityType/{entityType}/entity/{entity} | GET | Gets the groups that are mapped to the specified entity. |
| /api/v1/groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup} | POST | Adds a mapping between the specified groups. |
| /api/v1/groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup} | DELETE | Removes the mapping between the specified groups. |
| /api/v1/groupToGroupMappings/group/{group} | GET | Gets the groups that the specified group is mapped to. |
| /api/v1/groupToGroupReverseMappings/group/{group} | GET | Gets the groups that are mapped to the specified group. |
| /api/v1/users/{user} | POST | Adds a user. |
| /api/v1/users/{user} | DELETE | Removes a user. |
| /api/v1/users/{user} | GET | Returns the specified user if it exists. |
| /api/v1/userToGroupMappings/user/{user}/group/{group} | POST | Adds a mapping between the specified user and group. |
| /api/v1/userToGroupMappings/user/{user}/group/{group} | DELETE | Removes the mapping between the specified user and group. |
| /api/v1/userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | POST | Adds a mapping between the specified user, application component, and level of access to that component. |
| /api/v1/userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | DELETE | Removes a mapping between the specified user, application component, and level of access to that component. |
| /api/v1/userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity} | POST | Adds a mapping between the specified user, and entity. |
| /api/v1/userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity} | DELETE | Removes a mapping between the specified user, and entity. |
| /api/v1/users | GET | Returns all users. |
| /api/v1/userToGroupMappings/user/{user} | GET | Gets the groups that the specified user is mapped to (i.e. is a member of). |
| /api/v1/userToGroupMappings/group/{group} | GET | Gets the users that are mapped to the specified group. |
| /api/v1/userToApplicationComponentAndAccessLevelMappings/user/{user} | GET | Gets the application component and access level pairs that the specified user is mapped to. |
| /api/v1/userToApplicationComponentAndAccessLevelMappings/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | GET | Gets users that are mapped to the specific application component and access level pair. |
| /api/v1/userToEntityMappings/user/{user} | GET | Gets the entities that the specified user is mapped to. |
| /api/v1/userToEntityMappings/user/{user}/entityType/{entityType} | GET | Gets the entities of a given type that the specified user is mapped to. |
| /api/v1/userToEntityMappings/entityType/{entityType}/entity/{entity} | GET | Gets the users that are mapped to the specified entity. |
| /api/v1/dataElementAccess/applicationComponent/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel} | GET | Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access. |
| /api/v1/dataElementAccess/entity/user/{user}/entityType/{entityType}/entity/{entity} | GET | Checks whether the specified user (or a group that the user is a member of) has access to the specified entity. |