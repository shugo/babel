check_SCRIPTS		= test.rb

check_DATA		= bsc.exe \
			  bscore.dll \
			  bsbase.dll \
			  bsio.dll

TESTS			= test.rb

EXTRA_DIST		= test.rb.in \
			  absiter.out \
			  absiter.sa \
			  abstract.err \
			  abstract.sa \
			  conflict.err \
			  conflict.sa \
			  fact.out \
			  fact.sa \
			  hello.out \
			  hello.sa \
			  import.out \
			  import.sa \
			  iter.out \
			  iter.sa \
			  iterconflict.err \
			  iterconflict.sa \
			  new.out \
			  new.sa \
			  outarg.out \
			  outarg.sa \
			  subtype.out \
			  subtype.sa \
			  valuetype.out \
			  valuetype.sa \
			  caseinsensitive.out \
			  caseinsensitive.sa

CLEANFILES		= test.rb \
			  bsc.exe \
			  bscore.dll \
			  bsbase.dll \
			  bsio.dll \
			  absiter.exe \
			  fact.exe \
			  hello.exe \
			  iter.exe \
			  new.exe \
			  outarg.exe \
			  subtype.exe \
			  valuetype.exe \
			  caseinsensitive

test.rb: test.rb.in
	rm -f test.rb
	sed -e 's!%RUBY%!$(RUBY)!g' \
	    -e 's!%top_builddir%!$(top_builddir)!g' \
	    -e 's!%MONO%!$(MONO)!g' \
		$(srcdir)/test.rb.in > test.rb
	chmod +x test.rb
	chmod -w test.rb

bsc.exe: ../src/bsc.exe
	cp ../src/bsc.exe bsc.exe

bscore.dll: ../src/bscore.dll
	cp ../src/bscore.dll bscore.dll

bsbase.dll: ../lib/base/bsbase.dll
	cp ../lib/base/bsbase.dll bsbase.dll

bsio.dll: ../lib/io/bsio.dll
	cp ../lib/io/bsio.dll bsio.dll
