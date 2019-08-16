from enum import Enum


class Language(Enum):
	PYTHON3 = 'python3'

	@classmethod
	def has_value(cls, value):
		return value in cls._value2member_map_
