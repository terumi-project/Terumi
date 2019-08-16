from antlr4 import *
from antlr4_build.TerumiGrammarParser import *


class PythonTargetListener(ParseTreeListener):
	def __init__(self, output):
		self._output = output

	def write(self, data):
		self._output.write(data)

	def enterCompilation_unit(self, ctx: TerumiGrammarParser.Compilation_unitContext):
		self.write('entered compilation unit\n')

	def exitCompilation_unit(self, ctx: TerumiGrammarParser.Compilation_unitContext):
		self.write('exited compilation unit\n')

	def enterHello_message(self, ctx: TerumiGrammarParser.Hello_messageContext):
		self.write('entered some rule ' + ctx.IDENTIFIER().getText() + ' \n')

	def exitHello_message(self, ctx: TerumiGrammarParser.Hello_messageContext):
		self.write('exited some rule ' + ctx.IDENTIFIER().getText() + ' \n')
