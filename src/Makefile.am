bin_PROGRAMS		= babelc.exe

babelc_exe_SOURCES	= compiler.cs \
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
			  type.cs \
			  method.cs \
			  parameter.cs \
			  typemanager.cs \
			  typecreate.cs \
			  eltcreate.cs \
			  typecheck.cs \
			  codegen.cs

assembly_DATA		= Babel.Core.dll

Babel_Core_dll_SRCS	= attribute.cs \
			  bool.cs \
			  int.cs \
			  str.cs

EXTRA_DIST		= $(Babel_Core_dll_SRCS) \
			  parser.jay \
			  babelc.exe \
			  Babel.Core.dll

CLEANFILES		= babelc.exe \
			  Babel.Core.dll \
			  parser.cs \
			  y.output

ETAGS_ARGS		= $(Babel_Core_dll_SRCS) \
			  $(babelc_exe_SOURCES)

ETAGSFLAGS		= --c\#-kinds=-n --extra=+q

CTAGS_ARGS		= $(Babel_Core_dll_SRCS) \
			  $(babelc_exe_SOURCES)

CTAGSFLAGS		= --c\#-kinds=-n --extra=+q

babelc.exe: $(babelc_exe_SOURCES) Babel.Core.dll
	$(CS) $(CSFLAGS) -target:exe -out:babelc.exe \
		 -reference:Babel.Core.dll \
		$(babelc_exe_SOURCES)

parser.cs: parser.jay
	$(JAY) -ctv < $(SKELETON) $< > $@

Babel.Core.dll: $(Babel_Core_dll_SRCS)
	$(CS) $(CSFLAGS) -target:library \
		-out:Babel.Core.dll $(Babel_Core_dll_SRCS)
