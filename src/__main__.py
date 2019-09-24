import argparse
from Terumi import Terumi
from Languages import Language
import os


def main():
	parser = argparse.ArgumentParser(description='Python Terumi Compiler')

	parser.add_argument('-v', '--version', action='store_true', help='Prints the version', default=False)
	parser.add_argument('-o', '--output', nargs=1, help='The output directory', default='./output.ps1')
	parser.add_argument('-l', '--language', help='The desired output language.', default='python3')

	required_args = parser.add_argument_group('Required Arguments')
	required_args.add_argument('-i', '--input', help='The input terumi file')

	args = parser.parse_args()

	input = args.input
	output = args.output
	language = args.language

	if input is None:
		print("Input not specified. (-i file_name)")
		return

	if not Language.has_value(language):
		print("Language '" + language + "' is not a support language.")
		return

	# fix args
	cwd = os.getcwd()
	input = os.path.join(cwd, input)
	output = os.path.join(cwd, output)
	language = Language(language)

	instance = Terumi(
		input=input,
		output=output,
		language=language
	)

	instance.run()


if __name__ == '__main__':
	main()
