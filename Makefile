CS	= mcs

CSFLAGS	= -g

JAY	= jay

COMPILER_SRCS	= compiler.cs \
		  lexer.cs \
		  parser.cs \
		  node.cs \
		  program.cs \
		  source.cs \
		  class.cs \
		  statement.cs \
		  expression.cs \
		  typespec.cs \
		  local.cs \
		  location.cs \
		  report.cs \
		  parameter.cs \
		  typemanager.cs \
		  typecreate.cs \
		  eltcreate.cs \
		  typecheck.cs \
		  codegen.cs

BASE_LIB_SRCS	= attribute.cs \
		  bool.cs \
		  int.cs \
		  str.cs

COMPILER	= bsc.exe
BASE_LIB	= sacorlib.dll

all: $(COMPILER)

clean:
	rm -f $(COMPILER)
	rm -f $(BASE_LIB)
	rm -f parser.cs
	rm -f y.output
	rm -f *~

bsc.exe: $(COMPILER_SRCS) $(BASE_LIB)
	$(CS) $(CSFLAGS) -target:exe -out:bsc.exe \
		 -reference:$(BASE_LIB) \
		$(COMPILER_SRCS)

parser.cs: parser.jay skeleton.cs
	$(JAY) -ctv < skeleton.cs $< > $@

$(BASE_LIB): $(BASE_LIB_SRCS)
	$(CS) $(CSFLAGS) -target:library \
		-out:$(BASE_LIB) $(BASE_LIB_SRCS)
