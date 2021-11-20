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

from typing import TypeVar, Generic
from abc import ABC, abstractmethod

T = TypeVar("T")

class UniqueStringifierBase(Generic[T], ABC):
    """Base class for converting objects of a specified type to and from strings which uniquely identify the object.

    Generic Paramters:
        T:
            The type of objects to convert.
    """

    @abstractmethod
    def to_string(self, input_object: T) -> str:
        """Converts an object into a string which uniquely identifies that object.
        
        Args:
            input_object:
                The object to convert.
        
        Returns:
            A string which uniquely identifies that object.
        """

    @abstractmethod
    def from_string(self, stringified_object: str) -> T:
        """Converts a string which uniquely identifies an object into the object.
        
        Args:
            stringified_object:
                The string representing the object.
        
        Returns:
            The object.
        """
