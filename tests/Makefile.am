check_SCRIPTS		= test.rb

check_DATA		= babelc.exe \
			  bscore.dll \
			  bsbase.dll \
			  bsio.dll

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
			  bscore.dll \
			  bsbase.dll \
			  bsio.dll \
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

bscore.dll: ../src/bscore.dll
	cp ../src/bscore.dll bscore.dll

bsbase.dll: ../lib/base/bsbase.dll
	cp ../lib/base/bsbase.dll bsbase.dll

bsio.dll: ../lib/io/bsio.dll
	cp ../lib/io/bsio.dll bsio.dll
