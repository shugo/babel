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

CORE_LIB_SRCS	= attribute.cs \
		  bool.cs \
		  int.cs \
		  str.cs

BASE_LIB_SRCS	= lib/base/abstract.sa

IO_LIB_SRCS	= lib/io/stream.sa \
		  lib/io/out.sa

COMPILER	= bsc.exe
CORE_LIB	= sather-core.dll
BASE_LIB	= sather-base.dll
IO_LIB		= sather-io.dll
STD_LIBS	= $(BASE_LIB) \
		  $(IO_LIB)

all: $(COMPILER) $(STD_LIBS)

check: all
	cd tests; $(MAKE) check

clean:
	rm -f $(COMPILER)
	rm -f $(BASE_LIB)
	rm -f $(CORE_LIB)
	rm -f parser.cs
	rm -f y.output
	rm -f *~
	cd tests; $(MAKE) clean

bsc.exe: $(COMPILER_SRCS) $(CORE_LIB)
	$(CS) $(CSFLAGS) -target:exe -out:bsc.exe \
		 -reference:$(CORE_LIB) \
		$(COMPILER_SRCS)

parser.cs: parser.jay skeleton.cs
	$(JAY) -ctv < skeleton.cs $< > $@

$(CORE_LIB): $(CORE_LIB_SRCS)
	$(CS) $(CSFLAGS) -target:library \
		-out:$(CORE_LIB) $(CORE_LIB_SRCS)

$(BASE_LIB): $(BASE_LIB_SRCS) $(COMPILER)
	rm -f $(BASE_LIB)
	./$(COMPILER) -target:library \
		-out:$(BASE_LIB) $(BASE_LIB_SRCS)

$(IO_LIB): $(IO_LIB_SRCS) $(COMPILER)
	rm -f $(IO_LIB)
	./$(COMPILER) -target:library \
		-out:$(IO_LIB) $(IO_LIB_SRCS)
