check_SCRIPTS		= test.rb

check_DATA		= babelc.exe \
			  Babel.Core.dll \
			  Babel.Base.dll \
			  Babel.IO.dll

TESTS			= test.rb

EXTRA_DIST		= test.rb.in \
			  absiter.out \
			  absiter.sa \
			  abstract.err \
			  abstract.sa \
			  caseinsensitive.out \
			  caseinsensitive.sa \
			  conflict.err \
			  conflict.sa \
			  fact.out \
			  fact.sa \
			  hello.out \
			  hello.sa \
			  import.out \
			  import.sa \
			  if.out
			  if.sa
			  iter.out \
			  iter.sa \
			  iterconflict.err \
			  iterconflict.sa \
			  new.out \
			  new.sa \
			  newline.out \
			  newline.sa \
			  outarg.out \
			  outarg.sa \
			  subtype.out \
			  subtype.sa \
			  valuetype.out \
			  valuetype.sa

CLEANFILES		= test.rb \
			  babelc.exe \
			  Babel.Core.dll \
			  Babel.Base.dll \
			  Babel.IO.dll \
			  caseinsensitive.exe \
			  absiter.exe \
			  fact.exe \
			  hello.exe \
			  if.exe \
			  iter.exe \
			  new.exe \
			  newline.exe \
			  outarg.exe \
			  subtype.exe \
			  valuetype.exe

test.rb: test.rb.in
	rm -f test.rb
	sed -e 's!%RUBY%!$(RUBY)!g' \
	    -e 's!%top_builddir%!$(top_builddir)!g' \
	    -e 's!%MONO%!$(MONO)!g' \
		$(srcdir)/test.rb.in > test.rb
	chmod +x test.rb
	chmod -w test.rb

babelc.exe: ../src/babelc.exe
	cp ../src/babelc.exe babelc.exe

Babel.Core.dll: ../src/Babel.Core.dll
	cp ../src/Babel.Core.dll Babel.Core.dll

Babel.Base.dll: ../lib/base/Babel.Base.dll
	cp ../lib/base/Babel.Base.dll Babel.Base.dll

Babel.IO.dll: ../lib/io/Babel.IO.dll
	cp ../lib/io/Babel.IO.dll Babel.IO.dll
