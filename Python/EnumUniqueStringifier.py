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

from enum import Enum
from typing import TypeVar
from UniqueStringifierBase import UniqueStringifierBase

TEnum = TypeVar('TEnum', bound=Enum)

class EnumUniqueStringifier(UniqueStringifierBase[TEnum]):
    """An implementation of UniqueStringifierBase[T] for enums."""

    def to_string(self, input_object: TEnum) -> str:

        return str(input_object)

    def from_string(self, stringified_object: str) -> TEnum:

        print(globals())

        enum_type_and_value = stringified_object.split(".")
        print(enum_type_and_value)
        if len(enum_type_and_value) != 2:
            raise ValueError("String '{0}' could not be converted to an enum type.".format(stringified_object))
        #try:
        class_definition = globals()["AccessLevel"]
        #class_definition = globals()[enum_type_and_value[0]]
        return cls[enum_type_and_value[1]]
        #except Exception as e:
            #raise ValueError("String '{0}' could not be converted to an enum type.".format(stringified_object)) from e
