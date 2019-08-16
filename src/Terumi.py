from Languages import Language

from antlr4_build.TerumiGrammarLexer import TerumiGrammarLexer
from targets.PythonTargetListener import *


# TODO: setup different languages to trigger different target listeners
class Terumi:
	def __init__(self, input: str, output: str, language: Language):
		self._input = input
		self._output = output
		self._language = language

		self._input_stream = FileStream(self._input)
		self._lexer = TerumiGrammarLexer(self._input_stream)
		self._stream = CommonTokenStream(self._lexer)
		self._parser = TerumiGrammarParser(self._stream)

		self._output_file = open(self._output, "w+")
		self._listener = PythonTargetListener(self._output_file)
		self._walker = ParseTreeWalker()

	def run(self):
		tree = self._parser.compilation_unit()
		self._walker.walk(self._listener, tree)
