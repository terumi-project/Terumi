import subprocess
import os
import urllib.request

# setup antlr4 path
cwd = os.getcwd()
antlr4_directory = "antlr4_build"
path = os.path.join(cwd, antlr4_directory)

# make antlr4 dir
if not os.path.isdir(path):
	print("Setting up antlr4 directory")
	os.mkdir(path)

# download antlr4 jar
jar = 'https://www.antlr.org/download/antlr-4.7.2-complete.jar'
jar_name = 'antlr4.jar'
jar_path = os.path.join(path, jar_name)

if not os.path.isfile(jar_path):
	print("Downloading antlr4 jar")
	urllib.request.urlretrieve(jar, jar_path)

# execute antlr4 compile command
grammar_file = 'TerumiGrammar.g4'
grammar_path = os.path.join(cwd, grammar_file)

print("Executing antlr4 grammar build")
subprocess.run(["java", "-jar", jar_path, "-Dlanguage=Python3", grammar_path, "-o", path])
