#
# Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

from typing import TypeVar, Generic, Set, Dict, Tuple
from abc import ABC, abstractmethod
from collections.abc import Iterable

TUser = TypeVar("TUser")
TGroup = TypeVar("TGroup")
TComponent = TypeVar("TComponent")
TAccess = TypeVar("TAccess")

class AccessManager(Generic[TUser, TGroup, TComponent, TAccess]):
    """Manages the access of users and groups of users to components and entities within an application.

    Generic Paramters:
        TUser:
            The type of users in the application.
        TGroup:
            The type of groups in the application.
        TComponent:
            The type of components in the application to manage access to.
        TAccess:
            The type of levels of access which can be assigned to an application component.

    Attributes:
        users:
            Returns all users in the access manager.
        groups:
            Returns all groups in the access manager.

    """

    def __init__(self) -> None:
        """Initialises a new instance of the AccessManager class."""
        # Holds all users in the access manager
        self._users: Set[TUser] = set()
        # Holds all groups in the access manager
        self._groups: Set[TGroup] = set()
        # Holds graph edges which join users and groups within the access manager
        self._user_to_group_graph_edges: Dict[TUser, Set[TGroup]] = dict()
        # Holds graph edges which join groups and groups within the access manager
        self._group_to_group_graph_edges: Dict[TGroup, Set[TGroup]] = dict()
        # A dictionary which stores mappings between a user, and application component, and a level of access to that component
        self._user_to_component_map: Dict[TUser, Set[tuple[TComponent, TAccess]]] = dict()
        # A dictionary which stores mappings between a group, and application component, and a level of access to that component
        self._group_to_component_map: Dict[TGroup, Set[tuple[TComponent, TAccess]]] = dict()
        # Holds all valid entity types and values within the access manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).
        self._entities: Dict[str, Set[str]] = dict()
        # A dictionary which stores user to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the user.
        self._user_to_entity_map: Dict[TUser, Dict[str, Set[str]]] = dict()
        # A dictionary which stores group to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the group.
        self._group_to_entity_map: Dict[TGroup, Dict[str, Set[str]]] = dict()

    @property
    def users(self) -> Iterable[TUser]:
        """Returns all users in the access manager."""
        return self._users.__iter__()

    @property
    def groups(self) -> Iterable[TGroup]:
        """Returns all groups in the access manager."""
        return self._groups.__iter__()

    def add_user(self, user: TUser) -> None:
        """Adds a user.
        Args:
            user: 
                The user to add.
        """
        if user in self._users:
            raise ValueError("User '{0}' in argument 'user' already exists.".format(user))

        self._users.add(user)

    def contains_user(self, user: TUser) -> bool:
        """Returns true if the specified user exists.

        Args:
            user: 
                The user to check for.

        Returns:
            True if the specified user exists.
        """
        return user in self._users

    def remove_user(self, user: TUser) -> None:
        """Removes a user.

        Args:
            user: 
                The user to remove.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")

        if user in self._user_to_component_map:
            self._user_to_component_map.pop(user)
        if user in self._user_to_entity_map:
            self._user_to_entity_map.pop(user)
        self._users.remove(user)

    def add_group(self, group: TGroup) -> None:
        """Adds a group.
        Args:
            user: 
                The group to add.
        """
        if group in self._groups:
            raise ValueError("Group '{0}' in argument 'group' already exists.".format(group))

        self._groups.add(group)

    def contains_group(self, group: TGroup) -> bool:
        """Returns true if the specified group exists.

        Args:
            group: 
                The group to check for.

        Returns:
            True if the specified group exists.
        """
        return group in self._groups

    def remove_group(self, group: TGroup) -> None:
        """Removes a group.

        Args:
            user: 
                The group to remove.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")

        if group in self._group_to_component_map:
            self._group_to_component_map.pop(group)
        if group in self._group_to_entity_map:
            self._group_to_entity_map.pop(group)
        self._groups.remove(group)

    def add_user_to_group_mapping(self, user: TUser, group: TGroup) -> None:
        """Adds a mapping between the specified user and group.

        Args:
            user: 
                The user in the mapping.
            group: 
                The group in the mapping.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        if user in self._user_to_group_graph_edges:
            if group in self._user_to_group_graph_edges[user]:
                raise ValueError("A mapping between user '{0}' and group '{1}' already exists.".format(user, group))

        if user not in self._user_to_group_graph_edges:
            self._user_to_group_graph_edges[user] = set()
        self._user_to_group_graph_edges[user].add(group)

    def get_user_to_group_mappings(self, user: TUser) -> Iterable[TGroup]:
        """Gets the groups that the specified user is mapped to (i.e. is a member of).

        Args:
            user: 
                The user to retrieve the groups for.

        Returns:
            A collection of groups the specified user is a member of.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")

        if user in self._user_to_group_graph_edges:
            return self._user_to_group_graph_edges[user].__iter__()
        else:
            return iter(())

    def remove_user_to_group_mapping(self, user: TUser, group: TGroup) -> None:
        """Removes the mapping between the specified user and group.

        Args:
            user: 
                The user in the mapping.
            group: 
                The group in the mapping.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        if user not in self._user_to_group_graph_edges or group not in self._user_to_group_graph_edges[user]:
            raise ValueError("A mapping between user '{0}' and group '{1}' does not exist.".format(user, group))

        self._user_to_group_graph_edges[user].remove(group)

    def add_group_to_group_mapping(self, from_group: TGroup, to_group: TGroup) -> None:
        """Adds a mapping between the specified groups.

        Args:
            from_group: 
                The 'from' group in the mapping.
            to_group: 
                The 'to' group in the mapping.
        """
        if from_group not in self._groups:
            self._raise_group_doesnt_exist_error(from_group, "from_group")
        if to_group not in self._groups:
            self._raise_group_doesnt_exist_error(to_group, "to_group")
        if from_group == to_group:
            raise ValueError("Arguments 'from_group' and 'to_group' cannot contain the same group.")
        if from_group in self._group_to_group_graph_edges:
            if to_group in self._group_to_group_graph_edges[from_group]:
                raise ValueError("A mapping between group '{0}' and group '{1}' already exists.".format(from_group, to_group))

        # Check whether adding edge would create a circular reference
        circular_reference_check_action = self._CheckForCircularReferenceTraverserAction(from_group, "A mapping between groups '{0}' and '{1}' cannot be created as it would cause a circular reference.".format(str(from_group), str(to_group)), self._group_to_group_graph_edges)
        self._traverse_from_group_recurse(to_group, set(), circular_reference_check_action)
        if from_group not in self._group_to_group_graph_edges:
            self._group_to_group_graph_edges[from_group] = set()
        self._group_to_group_graph_edges[from_group].add(to_group)

    def get_group_to_group_mappings(self, group: TGroup) -> Iterable[TGroup]:
        """Gets the groups that the specified group is mapped to.

        Args:
            group: 
                The group to retrieve the mapped groups for.

        Returns:
            A collection of groups the specified group is mapped to.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")

        if group in self._group_to_group_graph_edges:
            return self._group_to_group_graph_edges[group].__iter__()
        else:
            return iter(())

    def remove_group_to_group_mapping(self, from_group: TGroup, to_group: TGroup) -> None:
        """Removes the mapping between the specified groups.

        Args:
            from_group: 
                The 'from' group in the mapping.
            to_group: 
                The 'to' group in the mapping.
        """
        if from_group not in self._groups:
            self._raise_group_doesnt_exist_error(from_group, "from_group")
        if to_group not in self._groups:
            self._raise_group_doesnt_exist_error(to_group, "to_group")
        if from_group not in self._group_to_group_graph_edges or to_group not in self._group_to_group_graph_edges[from_group]:
            raise ValueError("A mapping between groups '{0}' and '{1}' does not exist.".format(from_group, to_group))

        self._group_to_group_graph_edges[from_group].remove(to_group)

    def add_user_to_application_component_and_access_level_mapping(self, user: TUser, application_component: TComponent, access_level: TAccess) -> None:
        """Adds a mapping between the specified user, application component, and level of access to that component.

        Args:
            user: 
                The user in the mapping.
            application_component: 
                The application component in the mapping.
            access_level:
                The level of access to the component.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        
        component_and_access = (application_component, access_level)
        if user in self._user_to_component_map:
            if component_and_access in self._user_to_component_map[user]:
                raise ValueError("A mapping between user '{0}' application component '{1}' and access level '{2}' already exists.".format(user, application_component, access_level))
        else:
            self._user_to_component_map[user] = set()
        self._user_to_component_map[user].add(component_and_access)

    def get_user_to_application_component_and_access_level_mappings(self, user: TUser) -> Iterable[tuple[TComponent, TAccess]]:
        """Gets the application component and access level pairs that the specified user is mapped to.

        Args:
            user: 
                The user to retrieve the mappings for.

        Returns:
            A collection of tuples containing the application component and access level pairs that the specified user is mapped to.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")

        if user in self._user_to_component_map:
            return self._user_to_component_map[user].__iter__()
        else:
            return iter(())

    def remove_user_to_application_component_and_access_level_mapping(self, user: TUser, application_component: TComponent, access_level: TAccess) -> None:
        """Removes a mapping between the specified user, application component, and level of access to that component.

        Args:
            user: 
                The user in the mapping.
            application_component: 
                The application component in the mapping.
            access_level:
                The level of access to the component.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")

        component_and_access = (application_component, access_level)
        if user in self._user_to_component_map and component_and_access in self._user_to_component_map[user]:
            self._user_to_component_map[user].remove(component_and_access)
            if len(self._user_to_component_map[user]) == 0:
                self._user_to_component_map.remove(user)
        else:
            raise ValueError("A mapping between user '{0}' application component '{1}' and access level '{2}' doesn't exist.".format(user, application_component, access_level))

    def add_group_to_application_component_and_access_level_mapping(self, group: TGroup, application_component: TComponent, access_level: TAccess) -> None:
        """Adds a mapping between the specified group, application component, and level of access to that component.

        Args:
            group: 
                The group in the mapping.
            application_component: 
                The application component in the mapping.
            access_level:
                The level of access to the component.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        
        component_and_access = (application_component, access_level)
        if group in self._group_to_component_map:
            if component_and_access in self._group_to_component_map[group]:
                raise ValueError("A mapping between group '{0}' application component '{1}' and access level '{2}' already exists.".format(group, application_component, access_level))
        else:
            self._group_to_component_map[group] = set()
        self._group_to_component_map[group].add(component_and_access)

    def get_group_to_application_component_and_access_level_mappings(self, group: TGroup) -> Iterable[tuple[TComponent, TAccess]]:
        """Gets the application component and access level pairs that the specified group is mapped to.

        Args:
            group: 
                The group to retrieve the mappings for.

        Returns:
            A collection of tuples containing the application component and access level pairs that the specified group is mapped to.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")

        if group in self._group_to_component_map:
            return self._group_to_component_map[group].__iter__()
        else:
            return iter(())

    def remove_group_to_application_component_and_access_level_mapping(self, group: TGroup, application_component: TComponent, access_level: TAccess) -> None:
        """Removes a mapping between the specified group, application component, and level of access to that component.

        Args:
            group: 
                The group in the mapping.
            application_component: 
                The application component in the mapping.
            access_level:
                The level of access to the component.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")

        component_and_access = (application_component, access_level)
        if group in self._group_to_component_map and component_and_access in self._group_to_component_map[group]:
            self._group_to_component_map[group].remove(component_and_access)
            if len(self._group_to_component_map[group]) == 0:
                self._group_to_component_map.remove(group)
        else:
            raise ValueError("A mapping between group '{0}' application component '{1}' and access level '{2}' doesn't exist.".format(group, application_component, access_level))

    def add_entity_type(self, entity_type: str) -> None:
        """Adds an entity type.

        Args:
            entity_type: 
                The entity type to add.
        """
        if entity_type in self._entities:
            raise ValueError("Entity type '{0}' in argument 'entity_type' already exists.".format(entity_type))
        if len(entity_type) == 0 or entity_type.isspace() == True:
            raise ValueError("Entity type '{0}' in argument 'entity_type' must contain a valid character.".format(entity_type))

        self._entities[entity_type] = set()

    def contains_entity_type(self, entity_type: str) -> bool:
        """Returns true if the specified entity type exists.

        Args:
            entity_type: 
                The entity type to check for.

        Returns:
            True if the entity type exists.  False otherwise.
        """
        return entity_type in self._entities

    def remove_entity_type(self, entity_type: str) -> None:
        """Removes an entity type.

        Args:
            entity_type: 
                The entity type to remove.
        """
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")

        for current_user_entities in self._user_to_entity_map.values():
            if entity_type in current_user_entities:
                current_user_entities.pop(entity_type)
        for current_group_entities in self._group_to_entity_map.values():
            if entity_type in current_group_entities:
                current_group_entities.pop(entity_type)
        self._entities.pop(entity_type)

    def add_entity(self, entity_type: str, entity: str) -> None:
        """Adds an entity.

        Args:
            entity_type: 
                The type of the entity.
            entity: 
                The entity to add.
        """
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity in self._entities[entity_type]:
            raise ValueError("Entity '{0}' in argument 'entity' already exists.".format(entity))
        if len(entity) == 0 or entity.isspace() == True:
            raise ValueError("Entity '{0}' in argument 'entity' must contain a valid character.".format(entity))

        self._entities[entity_type].add(entity)

    def get_entities(self, entity_type: str) -> Iterable[str]:
        """Returns all entities of the specified type.

        Args:
            entity_type: 
                The type of the entity.

        Returns:
            A collection of all entities of the specified type.
        """
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        
        return self._entities[entity_type]

    def contains_entity(self, entity_type: str, entity: str) -> bool:
        """Returns true if the specified entity exists.

        Args:
            entity_type: 
                The type of the entity.
            entity:
                The entity to check for.

        Returns:
            True if the entity exists.  False otherwise.
        """

        return entity_type in self._entities and entity in self._entities[entity_type]

    def remove_entity(self, entity_type: str, entity: str) -> None:
        """Removes an entity.

        Args:
            entity_type: 
                The type of the entity.
            entity:
                The entity to remove.
        """
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")

        for current_user_entities in self._user_to_entity_map.values():
            if entity_type in current_user_entities and entity in current_user_entities[entity_type]:
                current_user_entities[entity_type].remove(entity)
        for current_group_entities in self._group_to_entity_map.values():
            if entity_type in current_group_entities and entity in current_group_entities[entity_type]:
                current_group_entities[entity_type].remove(entity)
        self._entities[entity_type].remove(entity)

    def add_user_to_entity_mapping(self, user: TUser, entity_type: str, entity: str) -> None:
        """Adds a mapping between the specified user, and entity.

        Args:
            user:
                The user in the mapping.
            entity_type: 
                The type of the entity.
            entity:
                The entity in the mapping.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")
        if user in self._user_to_entity_map and entity_type in self._user_to_entity_map[user] and entity in self._user_to_entity_map[user][entity_type]:
            raise ValueError("A mapping between user '{0}' and entity '{1}' with type '{2}' already exists.".format(user, entity, entity_type))

        if user not in self._user_to_entity_map:
            self._user_to_entity_map[user] = dict()
        if entity_type not in self._user_to_entity_map[user]:
            self._user_to_entity_map[user][entity_type] = set()

        self._user_to_entity_map[user][entity_type].add(entity)

    def get_user_to_entity_mappings_for_user(self, user: TUser) -> Iterable[tuple[str, str]]:
        """Gets the entities that the specified user is mapped to.

        Args:
            user:
                The user to retrieve the mappings for.

        Returns:
            A collection of tuples containing the entity type and entity that the specified user is mapped to.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")

        if user in self._user_to_entity_map:
            for current_entity_type in self._user_to_entity_map[user].items():
                for current_entity in current_entity_type[1]:
                    yield ( current_entity_type[0], current_entity )                  

    def get_user_to_entity_mappings_for_user_and_entity_type(self, user: TUser, entity_type: str) -> Iterable[str]:
        """Gets the entities of a given type that the specified user is mapped to.

        Args:
            user:
                The user to retrieve the mappings for.
            entity_type:
                The entity type to retrieve the mappings for.

        Returns:
            A collection of entities that the specified user is mapped to.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")

        if user in self._user_to_entity_map and entity_type in self._user_to_entity_map[user]:
            return self._user_to_entity_map[user][entity_type]
        else:
            return iter(())

    def remove_user_to_entity_mapping(self, user: TUser, entity_type: str, entity: str) -> None:
        """Removes a mapping between the specified user, and entity.

        Args:
            user:
                The user in the mapping.
            entity_type: 
                The type of the entity.
            entity:
                The entity in the mapping.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")
        if user not in self._user_to_entity_map:
            raise ValueError("A mapping between user '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(user, entity, entity_type))
        if user in self._user_to_entity_map and entity_type not in self._user_to_entity_map[user]:
            raise ValueError("A mapping between user '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(user, entity, entity_type))
        if user in self._user_to_entity_map and entity_type in self._user_to_entity_map[user] and entity not in self._user_to_entity_map[user][entity_type]:
            raise ValueError("A mapping between user '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(user, entity, entity_type))

        self._user_to_entity_map[user][entity_type].remove(entity)

    def add_group_to_entity_mapping(self, group: TGroup, entity_type: str, entity: str) -> None:
        """Adds a mapping between the specified group, and entity.

        Args:
            group:
                The group in the mapping.
            entity_type: 
                The type of the entity.
            entity:
                The entity in the mapping.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")
        if group in self._group_to_entity_map and entity_type in self._group_to_entity_map[group] and entity in self._group_to_entity_map[group][entity_type]:
            raise ValueError("A mapping between group '{0}' and entity '{1}' with type '{2}' already exists.".format(group, entity, entity_type))

        if group not in self._group_to_entity_map:
            self._group_to_entity_map[group] = dict()
        if entity_type not in self._group_to_entity_map[group]:
            self._group_to_entity_map[group][entity_type] = set()

        self._group_to_entity_map[group][entity_type].add(entity)

    def get_group_to_entity_mappings_for_group(self, group: TGroup) -> Iterable[tuple[str, str]]:
        """Gets the entities that the specified group is mapped to.

        Args:
            group:
                The group to retrieve the mappings for.

        Returns:
            A collection of tuples containing the entity type and entity that the specified group is mapped to.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")

        if group in self._group_to_entity_map:
            for current_entity_type in self._group_to_entity_map[group].items():
                for current_entity in current_entity_type[1]:
                    yield ( current_entity_type[0], current_entity )

    def get_group_to_entity_mappings_for_group_and_entity_type(self, group: TGroup, entity_type: str) -> Iterable[str]:
        """Gets the entities of a given type that the specified group is mapped to.

        Args:
            group:
                The group to retrieve the mappings for.
            entity_type:
                The entity type to retrieve the mappings for.

        Returns:
            A collection of entities that the specified group is mapped to.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")

        if group in self._group_to_entity_map and entity_type in self._group_to_entity_map[group]:
            return self._group_to_entity_map[group][entity_type]
        else:
            return iter(())

    def remove_group_to_entity_mapping(self, group: TGroup, entity_type: str, entity: str) -> None:
        """Removes a mapping between the specified group, and entity.

        Args:
            group:
                The group in the mapping.
            entity_type: 
                The type of the entity.
            entity:
                The entity in the mapping.
        """
        if group not in self._groups:
            self._raise_group_doesnt_exist_error(group, "group")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")
        if group not in self._group_to_entity_map:
            raise ValueError("A mapping between group '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(group, entity, entity_type))
        if group in self._group_to_entity_map and entity_type not in self._group_to_entity_map[group]:
            raise ValueError("A mapping between group '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(group, entity, entity_type))
        if group in self._group_to_entity_map and entity_type in self._group_to_entity_map[group] and entity not in self._group_to_entity_map[group][entity_type]:
            raise ValueError("A mapping between group '{0}' and entity '{1}' with type '{2}' doesn't exist.".format(group, entity, entity_type))

        self._group_to_entity_map[group][entity_type].remove(entity)

    def has_access_to_application_component(self, user: TUser, application_component: TComponent, access_level: TAccess) -> bool:
        """Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access.

        Args:
            user:
                The user to check for.
            application_component:
                The application component.
            access_level:
                The level of access to the component.

        Returns:
            True if the user has access the component.  False otherwise.
        """
        if user not in self._users:
            return False
        if user in self._user_to_component_map and ( application_component, access_level ) in self._user_to_component_map[user]:
            return True
        group_action = self._CheckForAccessTraverserAction(application_component, access_level, self._group_to_component_map)
        self._traverse_user_and_group_graph_from_user(user, group_action)

        return group_action.has_access

    def has_access_to_entity(self, user: TUser, entity_type: str, entity: str) -> bool:
        """Checks whether the specified user (or a group that the user is a member of) has access to the specified entity.

        Args:
            user:
                The user to check for.
            entity_type:
                The type of the entity.
            entity:
                The entity.

        Returns:
            True if the user has access the entity.  False otherwise.
        """
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")
        if entity not in self._entities[entity_type]:
            self._raise_entity_doesnt_exist_error(entity, "entity")

        if user not in self._users:
            return False
        if user in self._user_to_entity_map and entity_type in self._user_to_entity_map[user] and entity in self._user_to_entity_map[user][entity_type]:
            return True
        group_action = self._CheckForEntityMappingTraverserAction(entity_type, entity, self._group_to_entity_map)
        self._traverse_user_and_group_graph_from_user(user, group_action)

        return group_action._has_access

    def get_accessible_entities(self, user: TUser, entity_type: str) -> Iterable[str]:
        """Gets all entities of a given type that the specified user (or a group that the user is a member of) has access to.

        Args:
            user:
                The user to retrieve the entities for.
            entity_type:
                The type of entities to retrieve.

        Returns:
            The entities the user has access to.
        """
        if user not in self._users:
            self._raise_user_doesnt_exist_error(user, "user")
        if entity_type not in self._entities:
            self._raise_entity_type_doesnt_exist_error(entity_type, "entity_type")

        user_mapped_entities = set(self.get_user_to_entity_mappings_for_user_and_entity_type(user, entity_type))
        group_action = self._AddMappedEntitiesToSetTraverserAction(entity_type, self._group_to_entity_map)
        self._traverse_user_and_group_graph_from_user(user, group_action)

        return user_mapped_entities.union(group_action._mapped_entities).__iter__()

    #region Private/Protected Methods

    def _traverse_user_and_group_graph_from_user(self, start_user: TUser, group_action: "_UserToGroupGraphTraversalActionBase") -> None:
        """Traverses the user to group graph invoking the specified action at each group encountered.

        Args:
            start_user:
                The user to begin traversing at.
            group_action:
                The action to perform on each group.
        """
        if start_user not in self._users:
            self._raise_user_doesnt_exist_error(start_user, "start_user")

        if start_user in self._user_to_group_graph_edges:
            visited_groups = set()
            for next_edge_group in self._user_to_group_graph_edges[start_user]:
                self._traverse_from_group_recurse(next_edge_group, visited_groups, group_action)

    def _traverse_from_group_recurse(self, next_group: TGroup, visited_groups: Set[TGroup], group_action: "_UserToGroupGraphTraversalActionBase") -> bool:
        """Recurses to a group as part of a traversal, invoking the specified action.

        Args:
            next_group:
                The group to traverse to.
            visited_groups:
                The set of groups which have already been visited as part of the traversal.
            group_action:
                The action to perform on each group.
        """
        keep_traversing: bool = group_action.invoke(next_group)
        if next_group in self._group_to_group_graph_edges:
            for next_edge_group in self._group_to_group_graph_edges[next_group]:
                if next_edge_group not in visited_groups:
                    keep_traversing = self._traverse_from_group_recurse(next_edge_group, visited_groups, group_action)
                if keep_traversing == False:
                    break

        return keep_traversing

    def _raise_user_doesnt_exist_error(self, user: TUser, parameter_name: str) -> None:
        raise ValueError("User '{0}' in argument '{1}' does not exist.".format(user, parameter_name))

    def _raise_group_doesnt_exist_error(self, group: TGroup, parameter_name: str) -> None:
        raise ValueError("Group '{0}' in argument '{1}' does not exist.".format(group, parameter_name))

    def _raise_entity_type_doesnt_exist_error(self, entity_type: str, parameter_name: str) -> None:
        raise ValueError("Entity type '{0}' in argument '{1}' does not exist.".format(entity_type, parameter_name))

    def _raise_entity_doesnt_exist_error(self, entity: str, parameter_name: str) -> None:
        raise ValueError("Entity '{0}' in argument '{1}' does not exist.".format(entity, parameter_name))

    #endregion

    #region Inner Classes

    class _UserToGroupGraphTraversalActionBase(Generic[TGroup], ABC):
        """Base class for actions which should be performed on each group in the group hierarchy when traversing the graph of users and groups.

        Generic Paramters:
            TGroup:
                The type of groups in the application.
        """

        def __init__(self):
            """Initialises a new instance of the _UserToGroupGraphTraversalActionBase class."""

        @abstractmethod
        def invoke(self, group: TGroup) -> bool:
            """Executes the traversal action

            Args:
                group:
                    The group to perform the action on.
            
            Returns:
                Whether traversal should continue after the current group.
            """

    class _CheckForAccessTraverserAction(_UserToGroupGraphTraversalActionBase[TGroup]):
        """A user to group graph traversal action which checks whether any of the groups in the traversal path have access to a specified application component and access level.

        Attributes:
            has_access:
                Whether any of the groups have access to the application component and access level.
        """

        def __init__(self, application_component: TComponent, access_level: TAccess, group_to_component_map: Dict[TGroup, Set[tuple[TComponent, TAccess]]]) -> None:
            """Initialises a new instance of the _CheckForAccessTraverserAction class.

            Args:
                application_component:
                    The application component to check for.
                access_level:
                    The access level to check for.
                group_to_component_map:
                    The group to component map of the outer class.
            """
            self._application_component = application_component
            self._access_level = access_level
            # TODO: Remove this member if I can find a clean way to access members of the outer class
            self._group_to_component_map = group_to_component_map
            self._has_access = False

        @property
        def has_access(self) -> bool:
            """Whether any of the group have access to the application component and access level."""
            return self._has_access

        def invoke(self, group: TGroup) -> bool:
            if group in self._group_to_component_map and (self._application_component, self._access_level) in self._group_to_component_map[group]:
                self._has_access = True
                return False
            else:
                return True

    class _CheckForEntityMappingTraverserAction(_UserToGroupGraphTraversalActionBase[TGroup]):
        """A user to group graph traversal action which checks whether any of the groups in the traversal path are mapped to a specified entity.

        Attributes:
            has_access:
                Whether any of the groups are mapped to the entity.
        """

        def __init__(self, entity_type: str, entity: str, group_to_entity_map: Dict[TGroup, Dict[str, Set[str]]]) -> None:
            """Initialises a new instance of the _CheckForEntityMappingTraverserAction class.

            Args:
                entity_type:
                    The type of the entity to check for.
                entity:
                    The entity to check for.
                group_to_entity_map:
                    The group to entity map of the outer class.
            """
            self._entity_type = entity_type
            self._entity = entity
            # TODO: Remove this member if I can find a clean way to access members of the outer class
            self._group_to_entity_map = group_to_entity_map
            self._has_access = False

        @property
        def has_access(self) -> bool:
            """Whether any of the groups are mapped to the entity."""
            return self._has_access

        def invoke(self, group: TGroup) -> bool:
            if group in self._group_to_entity_map and self._entity_type in self._group_to_entity_map[group] and self._entity in self._group_to_entity_map[group][self._entity_type]:
                self._has_access = True
                return False
            else:
                return True

    class _AddMappedEntitiesToSetTraverserAction(_UserToGroupGraphTraversalActionBase[TGroup]):
        """A user to group graph traversal action which adds entities that are mapped to groups in the traversal path to a set.

        Attributes:
            mapped_entities:
                The entities which are mapped to groups in the traversal path.
        """

        @property
        def mapped_entities(self) -> bool:
            """The entities which are mapped to groups in the traversal path."""
            return self._mapped_entities

        def __init__(self, entity_type: str, group_to_entity_map: Dict[TGroup, Dict[str, Set[str]]]) -> None:
            """Initialises a new instance of the _AddMappedEntitiesToSetTraverserAction class.

            Args:
                entity_type:
                    The type of entities to add to the set.
                mapped_entities:
                    The set to add the entities to.
                group_to_entity_map:
                    The group to entity map of the outer class.
            """
            self._entity_type = entity_type
            self._mapped_entities = set()
            self._group_to_entity_map = group_to_entity_map

        def invoke(self, group: TGroup) -> bool:
            if group in self._group_to_entity_map and self._entity_type in self._group_to_entity_map[group]:
                self._mapped_entities = self._mapped_entities.union(self._group_to_entity_map[group][self._entity_type])

            return True

    class _CheckForCircularReferenceTraverserAction(_UserToGroupGraphTraversalActionBase[TGroup]):
        """A user to group graph traversal action which raises an exception if a circular reference is detected in the graph."""

        def __init__(self, check_group: TGroup, exception_message: str, group_to_group_graph_edges: Dict[TGroup, Set[TGroup]]) -> None:
            """Initialises a new instance of the _CheckForCircularReferenceTraverserAction class.

            Args:
                check_group:
                    The group to check for circular reference.
                exception_message:
                    The message to use in the raised exception if a circular reference is found.
                group_to_group_graph_edges:
                    The graph edges which join groups and groups of the outer class.
            """
            self._check_group = check_group
            self._exception_message = exception_message
            self._group_to_group_graph_edges = group_to_group_graph_edges

        def invoke(self, group: TGroup) -> bool:
            if group == self._check_group:
                raise Exception(self._exception_message)

            return True

    #endregion
