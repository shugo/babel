check_SCRIPTS		= test.rb

TESTS			= test.rb

EXTRA_DIST		= test.rb.in \
			  absiter.out \
			  absiter.sa \
			  abstract.err \
			  abstract.sa \
			  fact.out \
			  fact.sa \
			  hello.out \
			  hello.sa \
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
			  valuetype.sa

CLEANFILES		= test.rb

test.rb: test.rb.in
	rm -f test.rb
	sed -e 's!%RUBY%!$(RUBY)!g' \
	    -e 's!%top_builddir%!$(top_builddir)!g' \
	    -e 's!%MONO%!$(MONO)!g' \
		$(srcdir)/test.rb.in > test.rb
	chmod +x test.rb
	chmod -w test.rb
