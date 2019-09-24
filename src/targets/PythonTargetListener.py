from antlr4 import *
from antlr4_build.TerumiGrammarParser import *

class PythonTargetListener(ParseTreeListener):
	def __init__(self, output):
		self._output = output

	def write(self, data):
		self._output.write(data + '\n')
	
	def enterCompilation_unit(self, ctx:TerumiGrammarParser.Compilation_unitContext):
		self.write('// start')

	# Exit a parse tree produced by TerumiGrammarParser#compilation_unit.
	def exitCompilation_unit(self, ctx:TerumiGrammarParser.Compilation_unitContext):
		self.write('// end')


	# Enter a parse tree produced by TerumiGrammarParser#code_block.
	def enterCode_block(self, ctx:TerumiGrammarParser.Code_blockContext):
		self.write('{')

	# Exit a parse tree produced by TerumiGrammarParser#code_block.
	def exitCode_block(self, ctx:TerumiGrammarParser.Code_blockContext):
		self.write('}')


	# Enter a parse tree produced by TerumiGrammarParser#code_line.
	def enterCode_line(self, ctx:TerumiGrammarParser.Code_lineContext):
		compiler_reserved = ctx.COMPILER_RESERVED()
		if_statement = ctx.if_statement()
		variable_declaration = ctx.variable_declaration()
		function_declaration = ctx.function_declaration()

		if compiler_reserved is not None:
			function_call: TerumiGrammarParser.Function_callContext = ctx.function_call()
			keyword = function_call.NAME().getText()

			self.write('// COMPILER: ' + keyword)

			parameters: TerumiGrammarParser.ParametersContext = function_call.parameters()
			expression: TerumiGrammarParser.ExpressionContext = parameters.expression(0)
			self.write('// EXPRESSION: ' + expression.getText())

			if keyword == 'println':
				self.write('Write-Output ' + expression.getText())

	# Exit a parse tree produced by TerumiGrammarParser#code_line.
	def exitCode_line(self, ctx:TerumiGrammarParser.Code_lineContext):
		# self.write(sys._getframe().f_code.co_name)
		pass

	# Enter a parse tree produced by TerumiGrammarParser#if_statement.
	def enterIf_statement(self, ctx:TerumiGrammarParser.If_statementContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#if_statement.
	def exitIf_statement(self, ctx:TerumiGrammarParser.If_statementContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#function_call.
	def enterFunction_call(self, ctx:TerumiGrammarParser.Function_callContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#function_call.
	def exitFunction_call(self, ctx:TerumiGrammarParser.Function_callContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#parameters.
	def enterParameters(self, ctx:TerumiGrammarParser.ParametersContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#parameters.
	def exitParameters(self, ctx:TerumiGrammarParser.ParametersContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#variable_declaration.
	def enterVariable_declaration(self, ctx:TerumiGrammarParser.Variable_declarationContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#variable_declaration.
	def exitVariable_declaration(self, ctx:TerumiGrammarParser.Variable_declarationContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#expression.
	def enterExpression(self, ctx:TerumiGrammarParser.ExpressionContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#expression.
	def exitExpression(self, ctx:TerumiGrammarParser.ExpressionContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#string.
	def enterString(self, ctx:TerumiGrammarParser.StringContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#string.
	def exitString(self, ctx:TerumiGrammarParser.StringContext):
		self.write(sys._getframe().f_code.co_name)


	# Enter a parse tree produced by TerumiGrammarParser#number.
	def enterNumber(self, ctx:TerumiGrammarParser.NumberContext):
		self.write(sys._getframe().f_code.co_name)

	# Exit a parse tree produced by TerumiGrammarParser#number.
	def exitNumber(self, ctx:TerumiGrammarParser.NumberContext):
		self.write(sys._getframe().f_code.co_name)


