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

from typing import TypeVar, Generic
from AccessManager import AccessManager
from UniqueStringifierBase import UniqueStringifierBase

TUser = TypeVar("TUser")
TGroup = TypeVar("TGroup")
TComponent = TypeVar("TComponent")
TAccess = TypeVar("TAccess")

class AccessManagerJsonSerializer(Generic[TUser, TGroup, TComponent, TAccess]):
    """Serializes and deserializes an AccessManager to and from a JSON document."""

    def Serialize(
        access_manager: AccessManager[TUser, TGroup, TComponent, TAccess], 
        user_stringifier: UniqueStringifierBase[TUser], 
        group_stringifier: UniqueStringifierBase[TGroup], 
        application_component_stringifier: UniqueStringifierBase[TComponent], 
        access_level_stringifier: UniqueStringifierBase[TAccess]
    ) -> str:
        pass
