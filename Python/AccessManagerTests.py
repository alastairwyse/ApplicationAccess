#
# Copyright 2021 Alastair Wyse (https:#github.com/alastairwyse/ApplicationAccess/)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http:#www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#

from typing import Set, Dict
from enum import Enum, auto
import unittest

from AccessManager import AccessManager

class AccessManagerUnitTests(unittest.TestCase):
    """Unit tests for the AccessManager class."""

    def setUp(self):
        self._test_access_manager = AccessManager[str, str, self._ApplicationScreen, self._AccessLevel]()

    def test_users(self):
        all_users = set(self._test_access_manager.users)

        self.assertEqual(0, len(all_users))


        self._setup_test_users_and_groups(self._test_access_manager)

        all_users = set(self._test_access_manager.users)

        self.assertEqual(7, len(all_users))
        self.assertIn("Per1", all_users)
        self.assertIn("Per2", all_users)
        self.assertIn("Per3", all_users)
        self.assertIn("Per4", all_users)
        self.assertIn("Per5", all_users)
        self.assertIn("Per6", all_users)
        self.assertIn("Per7", all_users)

    def test_groups(self):
        all_groups = set(self._test_access_manager.groups)

        self.assertEqual(0, len(all_groups))


        self._setup_test_users_and_groups(self._test_access_manager)

        all_groups = set(self._test_access_manager.groups)

        self.assertEqual(4, len(all_groups))
        self.assertIn("Grp1", all_groups)
        self.assertIn("Grp2", all_groups)
        self.assertIn("Grp3", all_groups)
        self.assertIn("Grp4", all_groups)

    def test_add_user_user_already_exists(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user("user1")

        self.assertEqual("User 'user1' in argument 'user' already exists.", str(result.exception))

    def test_contains_user(self):
        self._setup_test_users_and_groups(self._test_access_manager)

        self.assertTrue(self._test_access_manager.contains_user("Per1"))
        self.assertFalse(self._test_access_manager.contains_user("Per8"))

    def test_remove_user_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user("user1")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_add_remove_user(self):
        self._test_access_manager.add_user("user1")

        self.assertTrue(self._test_access_manager.contains_user("user1"))
        

        self._test_access_manager.remove_user("user1")

        self.assertFalse(self._test_access_manager.contains_user("user1"))

    def test_remove_user(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")
        self._test_access_manager.add_user_to_group_mapping("user2", "group1")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user2", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self._test_access_manager.remove_user("user1")

        self.assertNotIn("user1", self._test_access_manager.users)
        self.assertIn("user2", self._test_access_manager.users)
        self.assertNotIn("user1", self._test_access_manager._user_to_component_map)
        self.assertIn("user2", self._test_access_manager._user_to_component_map)
        self.assertNotIn("user1", self._test_access_manager._user_to_entity_map)
        self.assertIn("user2", self._test_access_manager._user_to_entity_map)

    def test_add_group_group_already_exists(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group("group1")

        self.assertEqual("Group 'group1' in argument 'group' already exists.", str(result.exception))

    def test_contains_group(self):
        self._setup_test_users_and_groups(self._test_access_manager)

        self.assertTrue(self._test_access_manager.contains_group("Grp1"))
        self.assertFalse(self._test_access_manager.contains_group("Grp5"))

    def test_remove_group_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group("group1")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_add_remove_group(self):
        self._test_access_manager.add_group("group1")

        self.assertTrue(self._test_access_manager.contains_group("group1"))
        

        self._test_access_manager.remove_group("group1")

        self.assertFalse(self._test_access_manager.contains_group("group1"))

    def test_remove_group(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group2", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self._test_access_manager.remove_group("group1")

        self.assertNotIn("group1", self._test_access_manager.groups)
        self.assertIn("group2", self._test_access_manager.groups)
        self.assertNotIn("group1", self._test_access_manager._group_to_component_map)
        self.assertIn("group2", self._test_access_manager._group_to_component_map)
        self.assertNotIn("group1", self._test_access_manager._group_to_entity_map)
        self.assertIn("group2", self._test_access_manager._group_to_entity_map)

    def test_add_user_to_group_mapping_mapping_already_exists(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_group_mapping("user1", "group1")

        self.assertEqual("A mapping between user 'user1' and group 'group1' already exists.", str(result.exception))

    def test_add_user_to_group_mapping_user_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_group_mapping("user1", "group1")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_add_user_to_group_mapping_group_doesnt_exist(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_group_mapping("user1", "group1")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_get_user_to_group_mappings_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.get_user_to_group_mappings("user1")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_get_user_to_group_mappings(self):
        self._setup_test_users_and_groups(self._test_access_manager)

        all_mappings = set(self._test_access_manager.get_user_to_group_mappings("Per3"))

        self.assertEqual(2, len(all_mappings))
        self.assertIn("Grp1", all_mappings)
        self.assertIn("Grp2", all_mappings)


        self._test_access_manager.add_user("Per8")

        all_mappings = set(self._test_access_manager.get_user_to_group_mappings("Per8"))

        self.assertEqual(0, len(all_mappings))

    def test_remove_user_to_group_mapping_user_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_group_mapping("user1", "group1")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_remove_user_to_group_mapping_group_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_group_mapping("user1", "group1")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_remove_user_to_group_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_group("group1")
        
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_group_mapping("user1", "group1")

        self.assertEqual("A mapping between user 'user1' and group 'group1' does not exist.", str(result.exception))

    def test_remove_user_to_group_mapping(self):
        self._setup_test_users_and_groups(self._test_access_manager)
        self.assertIn("Grp1", self._test_access_manager.get_user_to_group_mappings("Per1"))

        self._test_access_manager.remove_user_to_group_mapping("Per1", "Grp1")

        self.assertNotIn("Grp1", self._test_access_manager.get_user_to_group_mappings("Per1"))

    def test_add_group_to_group_mapping_to_group_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_group_mapping("group1", "group2")

        self.assertEqual("Group 'group2' in argument 'to_group' does not exist.", str(result.exception))

    def test_add_group_to_group_mapping_from_group_doesnt_exist(self):
        self._test_access_manager.add_group("group2")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_group_mapping("group1", "group2")

        self.assertEqual("Group 'group1' in argument 'from_group' does not exist.", str(result.exception))

    def test_add_group_to_group_mapping_mapping_already_exists(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group_to_group_mapping("group1", "group2")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_group_mapping("group1", "group2")

        self.assertEqual("A mapping between group 'group1' and group 'group2' already exists.", str(result.exception))

    def test_add_group_to_group_mapping_to_and_from_groups_are_same(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_group_mapping("group2", "group2")

        self.assertEqual("Arguments 'from_group' and 'to_group' cannot contain the same group.", str(result.exception))

    def test_add_group_to_group_mapping_adding_creates_circular_reference(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_group("group4")
        self._test_access_manager.add_group_to_group_mapping("group1", "group2")
        self._test_access_manager.add_group_to_group_mapping("group2", "group3")
        self._test_access_manager.add_group_to_group_mapping("group3", "group4")

        with self.assertRaises(Exception) as result:
            self._test_access_manager.add_group_to_group_mapping("group3", "group1")

        self.assertEqual("A mapping between groups 'group3' and 'group1' cannot be created as it would cause a circular reference.", str(result.exception))

    def test_get_group_to_group_mappings_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.get_group_to_group_mappings("group1")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_get_group_to_group_mappings(self):
        self._setup_test_users_and_groups(self._test_access_manager)

        all_mappings = set(self._test_access_manager.get_group_to_group_mappings("Grp1"))

        self.assertEqual(2, len(all_mappings))
        self.assertIn("Grp4", all_mappings)
        self.assertIn("Grp3", all_mappings)


        all_mappings = set(self._test_access_manager.get_group_to_group_mappings("Grp3"))

        self.assertEqual(0, len(all_mappings))

    def test_remove_group_to_group_mapping_to_group_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_group_mapping("group1", "group2")

        self.assertEqual("Group 'group2' in argument 'to_group' does not exist.", str(result.exception))

    def test_remove_group_to_group_mapping_from_group_doesnt_exist(self):
        self._test_access_manager.add_group("group2")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_group_mapping("group1", "group2")

        self.assertEqual("Group 'group1' in argument 'from_group' does not exist.", str(result.exception))

    def test_remove_group_to_group_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_group_mapping("group1", "group2")

        self.assertEqual("A mapping between groups 'group1' and 'group2' does not exist.", str(result.exception))

    def test_remove_group_to_group_mapping(self):
        self._setup_test_users_and_groups(self._test_access_manager)
        self.assertIn("Grp3", self._test_access_manager.get_group_to_group_mappings("Grp2"))

        self._test_access_manager.remove_group_to_group_mapping("Grp2", "Grp3")

        self.assertNotIn("Grp3", self._test_access_manager.get_group_to_group_mappings("Grp2"))

    def test_add_user_to_application_component_and_access_level_mapping_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_add_user_to_application_component_and_access_level_mapping_mapping_already_exists(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("A mapping between user 'user1' application component '_ApplicationScreen.ORDER' and access level '_AccessLevel.CREATE' already exists.", str(result.exception))

    def test_get_user_to_application_component_and_access_level_mappings_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.get_user_to_application_component_and_access_level_mappings("user1")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_get_user_to_application_component_and_access_level_mappings(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_group("group1")

        all_mappings = set(self._test_access_manager.get_user_to_application_component_and_access_level_mappings("user1"))

        self.assertEqual(0, len(all_mappings))


        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.SUMMARY, self._AccessLevel.VIEW)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.MODIFY)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user2", self._ApplicationScreen.SETTINGS, self._AccessLevel.VIEW)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user2", self._ApplicationScreen.SETTINGS, self._AccessLevel.CREATE)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user2", self._ApplicationScreen.SETTINGS, self._AccessLevel.MODIFY)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.VIEW)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.CREATE)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.MODIFY)

        all_mappings = set(self._test_access_manager.get_user_to_application_component_and_access_level_mappings("user1"))
        
        self.assertEqual(4, len(all_mappings))
        self.assertIn(( self._ApplicationScreen.ORDER, self._AccessLevel.VIEW ), all_mappings)
        self.assertIn(( self._ApplicationScreen.SUMMARY, self._AccessLevel.VIEW ), all_mappings)
        self.assertIn(( self._ApplicationScreen.ORDER, self._AccessLevel.CREATE ), all_mappings)
        self.assertIn(( self._ApplicationScreen.ORDER, self._AccessLevel.MODIFY ), all_mappings)

    def test_remove_user_to_application_component_and_access_level_mapping_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_remove_user_to_application_component_and_access_level_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("A mapping between user 'user1' application component '_ApplicationScreen.ORDER' and access level '_AccessLevel.CREATE' doesn't exist.", str(result.exception))

    def test_add_group_to_application_component_and_access_level_mapping_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_add_group_to_application_component_and_access_level_mapping_mapping_already_exists(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("A mapping between group 'group1' application component '_ApplicationScreen.ORDER' and access level '_AccessLevel.CREATE' already exists.", str(result.exception))

    def test_get_group_to_application_component_and_access_level_mappings_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.get_group_to_application_component_and_access_level_mappings("group1")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_get_group_to_application_component_and_access_level_mappings(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")

        all_mappings = set(self._test_access_manager.get_group_to_application_component_and_access_level_mappings("group1"))

        self.assertEqual(0, len(all_mappings))


        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.SUMMARY, self._AccessLevel.VIEW)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.MODIFY)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.VIEW)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.CREATE)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.MODIFY)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.SUMMARY, self._AccessLevel.VIEW)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group2", self._ApplicationScreen.SETTINGS, self._AccessLevel.VIEW)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group2", self._ApplicationScreen.SETTINGS, self._AccessLevel.CREATE)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group2", self._ApplicationScreen.SETTINGS, self._AccessLevel.MODIFY)

        all_mappings = set(self._test_access_manager.get_group_to_application_component_and_access_level_mappings("group1"))
        
        self.assertEqual(4, len(all_mappings))
        self.assertIn(( self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.VIEW ), all_mappings)
        self.assertIn(( self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.CREATE ), all_mappings)
        self.assertIn(( self._ApplicationScreen.MANAGE_PRODUCTS, self._AccessLevel.MODIFY ), all_mappings)
        self.assertIn(( self._ApplicationScreen.SUMMARY, self._AccessLevel.VIEW ), all_mappings)

    def test_remove_group_to_application_component_and_access_level_mapping_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_remove_group_to_application_component_and_access_level_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_application_component_and_access_level_mapping("group1", self._ApplicationScreen.ORDER, self._AccessLevel.CREATE)

        self.assertEqual("A mapping between group 'group1' application component '_ApplicationScreen.ORDER' and access level '_AccessLevel.CREATE' doesn't exist.", str(result.exception))

    def test_add_entity_type_entity_type_already_exists(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_entity_type("ClientAccount")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' already exists.", str(result.exception))

    def test_add_entity_type_blank_name(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_entity_type("")

        self.assertEqual("Entity type '' in argument 'entity_type' must contain a valid character.", str(result.exception))

    def test_contains_entity_type(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        self.assertTrue(self._test_access_manager.contains_entity_type("ClientAccount"))
        self.assertFalse(self._test_access_manager.contains_entity_type("BusinessUnit"))

    def test_remove_entity_type_entity_type_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_entity_type("ClientAccount")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_remove_entity_type(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user2", "BusinessUnit", "Sales")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")

        self._test_access_manager.remove_entity_type("ClientAccount")

        for current_user_entities in self._test_access_manager._user_to_entity_map.values():
            self.assertNotIn("ClientAccount", current_user_entities)
        for current_group_entities in self._test_access_manager._group_to_entity_map.values():
            self.assertNotIn("ClientAccount", current_group_entities)
        mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user1"))
        self.assertEqual(1, len(mappings))
        self.assertIn( ( "BusinessUnit", "Marketing" ), mappings )
        mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user2"))
        self.assertEqual(1, len(mappings))
        self.assertIn( ( "BusinessUnit", "Sales" ), mappings )
        mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group("group1"))
        self.assertEqual(1, len(mappings))
        self.assertIn( ( "BusinessUnit", "Marketing" ), mappings )

    def test_add_entity_entity_type_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_entity("ClientAccount", "CompanyA")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_add_entity_entity_already_exists(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_entity("ClientAccount", "CompanyA")

        self.assertEqual("Entity 'CompanyA' in argument 'entity' already exists.", str(result.exception))

    def test_add_entity_blank_name(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_entity("ClientAccount", "  ")

        self.assertEqual("Entity '  ' in argument 'entity' must contain a valid character.", str(result.exception))

    def test_get_entities_entity_type_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.get_entities("ClientAccount")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_contains_entity(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")

        self.assertTrue(self._test_access_manager.contains_entity("ClientAccount", "CompanyA"))
        self.assertFalse(self._test_access_manager.contains_entity("ClientAccount", "CompanyB"))
        self.assertFalse(self._test_access_manager.contains_entity("BusinessUnit", "Marketing"))

    def test_remove_entity_entity_type_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_entity("ClientAccount", "CompanyA")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_remove_entity_entity_doesnt_exist(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_entity("ClientAccount", "CompanyA")

        self.assertEqual("Entity 'CompanyA' in argument 'entity' does not exist.", str(result.exception))
 
    def test_remove_entity(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user2", "BusinessUnit", "Sales")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")

        self._test_access_manager.remove_entity("ClientAccount", "CompanyB")

        for current_user_entities in self._test_access_manager._user_to_entity_map.values():
            if "ClientAccount" in current_user_entities:
                self.assertNotIn("CompanyB", current_user_entities["ClientAccount"])
        for current_group_entities in self._test_access_manager._group_to_entity_map.values():
            if "ClientAccount" in current_group_entities:
                self.assertNotIn("CompanyB", current_group_entities["ClientAccount"])
        mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user1"))
        self.assertEqual(2, len(mappings))
        self.assertIn( ( "ClientAccount", "CompanyA" ), mappings )
        self.assertIn( ( "BusinessUnit", "Marketing" ), mappings )
        mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user2"))
        self.assertEqual(2, len(mappings))
        self.assertIn( ( "ClientAccount", "CompanyA" ), mappings )
        self.assertIn( ( "BusinessUnit", "Sales" ), mappings )
        mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group("group1"))
        self.assertEqual(1, len(mappings))
        self.assertIn( ( "BusinessUnit", "Marketing" ), mappings )

    def test_add_user_to_entity_mapping_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_add_user_to_entity_mapping_entity_type_doesnt_exist(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_add_user_to_entity_mapping_entity_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("Entity 'CompanyA' in argument 'entity' does not exist.", str(result.exception))

    def test_add_user_to_entity_mapping_mapping_already_exists(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between user 'user1' and entity 'CompanyA' with type 'ClientAccount' already exists.", str(result.exception))

    def test_get_user_to_entity_mappings_for_user_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_user_to_entity_mappings_for_user("user1"):
                pass

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_get_user_to_entity_mappings_for_user(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user2", "BusinessUnit", "Sales")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")

        all_mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user3"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user("user1"))

        self.assertEqual(3, len(all_mappings))
        self.assertIn(( "ClientAccount", "CompanyA" ), all_mappings)
        self.assertIn(( "ClientAccount", "CompanyB" ), all_mappings)
        self.assertIn(( "BusinessUnit", "Marketing" ), all_mappings)

    def test_get_user_to_entity_mappings_for_user_and_entity_type_user_doesnt_exist(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_user_to_entity_mappings_for_user_and_entity_type("user1", "ClientAccount"):
                pass

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_get_user_to_entity_mappings_for_user_and_entity_type_entity_type_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_user_to_entity_mappings_for_user_and_entity_type("user1", "BusinessUnit"):
                pass

        self.assertEqual("Entity type 'BusinessUnit' in argument 'entity_type' does not exist.", str(result.exception))

    def test_get_user_to_entity_mappings_for_user_and_entity_type(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity_type("ProductType")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user2", "BusinessUnit", "Sales")

        all_mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user_and_entity_type("user3", "ClientAccount"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user_and_entity_type("user1", "ProductType"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_user_to_entity_mappings_for_user_and_entity_type("user1", "ClientAccount"))

        self.assertEqual(2, len(all_mappings))
        self.assertIn("CompanyA", all_mappings)
        self.assertIn("CompanyB", all_mappings)

    def test_remove_user_to_entity_mapping_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_remove_user_to_entity_mapping_entity_type_doesnt_exist(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_remove_user_to_entity_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between user 'user2' and entity 'CompanyA' with type 'ClientAccount' doesn't exist.", str(result.exception))


        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")

        self.assertEqual("A mapping between user 'user1' and entity 'Marketing' with type 'BusinessUnit' doesn't exist.", str(result.exception))


        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between user 'user1' and entity 'CompanyA' with type 'ClientAccount' doesn't exist.", str(result.exception))

    def test_add_group_to_entity_mapping_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_add_group_to_entity_mapping_entity_type_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_add_group_to_entity_mapping_entity_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("Entity 'CompanyA' in argument 'entity' does not exist.", str(result.exception))

    def test_add_group_to_entity_mapping_mapping_already_exists(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between group 'group1' and entity 'CompanyA' with type 'ClientAccount' already exists.", str(result.exception))

    def test_get_group_to_entity_mappings_for_group_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_group_to_entity_mappings_for_group("group1"):
                pass

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))
    
    def test_get_group_to_entity_mappings_for_group(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_group("group1")        
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group2", "BusinessUnit", "Sales")

        all_mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group("group3"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group("group2"))

        self.assertEqual(3, len(all_mappings))
        self.assertIn(( "ClientAccount", "CompanyA" ), all_mappings)
        self.assertIn(( "ClientAccount", "CompanyB" ), all_mappings)
        self.assertIn(( "BusinessUnit", "Sales" ), all_mappings)

    def test_get_group_to_entity_mappings_for_group_and_entity_group_doesnt_exist(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_group_to_entity_mappings_for_group_and_entity_type("group1", "ClientAccount"):
                pass

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_get_group_to_entity_mappings_for_group_and_entity_type_entity_type_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            for current_mapping in self._test_access_manager.get_group_to_entity_mappings_for_group_and_entity_type("group1", "BusinessUnit"):
                pass

        self.assertEqual("Entity type 'BusinessUnit' in argument 'entity_type' does not exist.", str(result.exception))

    def test_get_group_to_entity_mappings_for_group_and_entity_type(self):
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity_type("ProductType")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_entity("BusinessUnit", "Sales")
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_user_to_entity_mapping("user1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_group_to_entity_mapping("group2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group2", "BusinessUnit", "Sales")

        all_mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group_and_entity_type("group3", "ClientAccount"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group_and_entity_type("group1", "ProductType"))

        self.assertEqual(0, len(all_mappings))


        all_mappings = set(self._test_access_manager.get_group_to_entity_mappings_for_group_and_entity_type("group2", "ClientAccount"))

        self.assertEqual(2, len(all_mappings))
        self.assertIn("CompanyA", all_mappings)
        self.assertIn("CompanyB", all_mappings)

    def test_remove_group_to_entity_mapping_group_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("Group 'group1' in argument 'group' does not exist.", str(result.exception))

    def test_remove_group_to_entity_mapping_entity_type_doesnt_exist(self):
        self._test_access_manager.add_group("group1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")
            
        self.assertEqual("Entity type 'ClientAccount' in argument 'entity_type' does not exist.", str(result.exception))

    def test_remove_group_to_entity_mapping_entity_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")
            
        self.assertEqual("Entity 'CompanyA' in argument 'entity' does not exist.", str(result.exception))

    def test_remove_group_to_entity_mapping_mapping_doesnt_exist(self):
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group1", "ClientAccount", "CompanyB")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group2", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between group 'group2' and entity 'CompanyA' with type 'ClientAccount' doesn't exist.", str(result.exception))


        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group1", "BusinessUnit", "Marketing")

        self.assertEqual("A mapping between group 'group1' and entity 'Marketing' with type 'BusinessUnit' doesn't exist.", str(result.exception))


        with self.assertRaises(ValueError) as result:
            self._test_access_manager.remove_group_to_entity_mapping("group1", "ClientAccount", "CompanyA")

        self.assertEqual("A mapping between group 'group1' and entity 'CompanyA' with type 'ClientAccount' doesn't exist.", str(result.exception))

    def test_has_access_to_application_component_user_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")
        self._test_access_manager.add_user_to_group_mapping("user2", "group1")
        self._test_access_manager.add_user_to_group_mapping("user3", "group2")

        result: bool =  self._test_access_manager.has_access_to_application_component("user4", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)

        self.assertFalse(result)

    def test_has_access_to_application_component_user_has_access(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user_to_application_component_and_access_level_mapping("user1", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)

        result: bool =  self._test_access_manager.has_access_to_application_component("user1", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)

        self.assertTrue(result)


        result: bool =  self._test_access_manager.has_access_to_application_component("user2", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)

        self.assertFalse(result)

    def test_has_access_to_application_component_group_has_access(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")
        self._test_access_manager.add_user_to_group_mapping("user2", "group1")
        self._test_access_manager.add_user_to_group_mapping("user3", "group2")
        self._test_access_manager.add_group_to_group_mapping("group2", "group3")
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group3", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)
        self._test_access_manager.add_group_to_application_component_and_access_level_mapping("group2", self._ApplicationScreen.SETTINGS, self._AccessLevel.MODIFY)

        result: bool = self._test_access_manager.has_access_to_application_component("user3", self._ApplicationScreen.ORDER, self._AccessLevel.VIEW)

        self.assertTrue(result)


        result: bool = self._test_access_manager.has_access_to_application_component("user3", self._ApplicationScreen.SETTINGS, self._AccessLevel.MODIFY)

        self.assertTrue(result)


        result: bool = self._test_access_manager.has_access_to_application_component("user1", self._ApplicationScreen.SETTINGS, self._AccessLevel.MODIFY)

        self.assertFalse(result)

    def test_has_access_to_entity_user_doesnt_exist(self):
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")

        result = self._test_access_manager.has_access_to_entity("user1", "BusinessUnit", "Marketing")

        self.assertFalse(result)

    def test_has_access_to_entity_entity_type_doesnt_exist(self):
        self._test_access_manager.add_user("user1")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.has_access_to_entity("user1", "BusinessUnit", "Marketing")

        self.assertEqual("Entity type 'BusinessUnit' in argument 'entity_type' does not exist.", str(result.exception))

    def test_has_access_to_entity_entity_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_entity_type("BusinessUnit")

        with self.assertRaises(ValueError) as result:
            self._test_access_manager.has_access_to_entity("user1", "BusinessUnit", "Marketing")

        self.assertEqual("Entity 'Marketing' in argument 'entity' does not exist.", str(result.exception))

    def test_has_access_to_entity_user_has_access(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyB")

        result: bool = self._test_access_manager.has_access_to_entity("user1", "ClientAccount", "CompanyB")

        self.assertTrue(result)


        result: bool = self._test_access_manager.has_access_to_entity("user2", "ClientAccount", "CompanyB")

        self.assertFalse(result)

    def test_has_access_to_entity_group_has_access(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")
        self._test_access_manager.add_user_to_group_mapping("user2", "group1")
        self._test_access_manager.add_user_to_group_mapping("user3", "group2")
        self._test_access_manager.add_group_to_group_mapping("group2", "group3")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_group_to_entity_mapping("group3", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group2", "BusinessUnit", "Marketing")


        result: bool = self._test_access_manager.has_access_to_entity("user3", "ClientAccount", "CompanyB")

        self.assertTrue(result)


        result: bool = self._test_access_manager.has_access_to_entity("user3", "BusinessUnit", "Marketing")

        self.assertTrue(result)


        result: bool = self._test_access_manager.has_access_to_entity("user1", "BusinessUnit", "Marketing")

        self.assertFalse(result)

    def test_get_accessible_entities_user_doesnt_exist(self):
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            all_entities = set(self._test_access_manager.get_accessible_entities("user1", "ClientAccount"))

        self.assertEqual("User 'user1' in argument 'user' does not exist.", str(result.exception))

    def test_get_accessible_entities_entity_type_doesnt_exist(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_entity_type("ClientAccount")

        with self.assertRaises(ValueError) as result:
            all_entities = set(self._test_access_manager.get_accessible_entities("user1", "BusinessUnit"))

        self.assertEqual("Entity type 'BusinessUnit' in argument 'entity_type' does not exist.", str(result.exception))

    def test_get_accessible_entities(self):
        self._test_access_manager.add_user("user1")
        self._test_access_manager.add_user("user2")
        self._test_access_manager.add_user("user3")
        self._test_access_manager.add_group("group1")
        self._test_access_manager.add_group("group2")
        self._test_access_manager.add_group("group3")
        self._test_access_manager.add_user_to_group_mapping("user1", "group1")
        self._test_access_manager.add_user_to_group_mapping("user2", "group2")
        self._test_access_manager.add_user_to_group_mapping("user3", "group2")
        self._test_access_manager.add_group_to_group_mapping("group2", "group3")
        self._test_access_manager.add_entity_type("ClientAccount")
        self._test_access_manager.add_entity_type("BusinessUnit")
        self._test_access_manager.add_entity("ClientAccount", "CompanyA")
        self._test_access_manager.add_entity("ClientAccount", "CompanyB")
        self._test_access_manager.add_entity("ClientAccount", "CompanyC")
        self._test_access_manager.add_entity("ClientAccount", "CompanyD")
        self._test_access_manager.add_entity("BusinessUnit", "Marketing")
        self._test_access_manager.add_user_to_entity_mapping("user1", "ClientAccount", "CompanyD")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyA")
        self._test_access_manager.add_user_to_entity_mapping("user2", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group3", "ClientAccount", "CompanyB")
        self._test_access_manager.add_group_to_entity_mapping("group3", "ClientAccount", "CompanyC")
        self._test_access_manager.add_group_to_entity_mapping("group2", "BusinessUnit", "Marketing")

        all_entities = set(self._test_access_manager.get_accessible_entities("user2", "ClientAccount"))

        self.assertEqual(3, len(all_entities))
        self.assertIn("CompanyA", all_entities)
        self.assertIn("CompanyB", all_entities)
        self.assertIn("CompanyC", all_entities)

    def test_traverse_user_and_group_graph_from_user_user_doesnt_exist(self):
        with self.assertRaises(ValueError) as result:
            self._test_access_manager._traverse_user_and_group_graph_from_user("user1", None)

        self.assertEqual("User 'user1' in argument 'start_user' does not exist.", str(result.exception))

    #region Private/Protected Methods

    # Creates the following user to group mapping
    #
    #   Grp4         Grp3-------------
    #      \       /       \          \   
    #       \    /           \         \ 
    #        Grp1      --------Grp2     \
    #      /  |   \   /      /  |   \    \
    #  Per1 Per2  Per3 Per4  Per5 Per6  Per7
    #
    def _setup_test_users_and_groups(self, access_manager) -> None:
        access_manager.add_user("Per1")
        access_manager.add_user("Per2")
        access_manager.add_user("Per3")
        access_manager.add_user("Per4")
        access_manager.add_user("Per5")
        access_manager.add_user("Per6")
        access_manager.add_user("Per7")
        access_manager.add_group("Grp1")
        access_manager.add_group("Grp2")
        access_manager.add_group("Grp3")
        access_manager.add_group("Grp4")
        access_manager.add_user_to_group_mapping("Per1", "Grp1")
        access_manager.add_user_to_group_mapping("Per2", "Grp1")
        access_manager.add_user_to_group_mapping("Per3", "Grp1")
        access_manager.add_user_to_group_mapping("Per3", "Grp2")
        access_manager.add_user_to_group_mapping("Per4", "Grp2")
        access_manager.add_user_to_group_mapping("Per5", "Grp2")
        access_manager.add_user_to_group_mapping("Per6", "Grp2")
        access_manager.add_user_to_group_mapping("Per7", "Grp3")
        access_manager.add_group_to_group_mapping("Grp1", "Grp4")
        access_manager.add_group_to_group_mapping("Grp1", "Grp3")
        access_manager.add_group_to_group_mapping("Grp2", "Grp3")

    #endregion

    #region Inner Classes

    class _ApplicationScreen(Enum):
        ORDER = auto()
        SUMMARY = auto()
        MANAGE_PRODUCTS = auto()
        SETTINGS = auto()

    class _AccessLevel(Enum):
        VIEW = auto()
        CREATE = auto()
        MODIFY = auto()
        DELETE = auto()

    #endregion

if __name__ == "__main__":
    unittest.main()