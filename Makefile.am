SUBDIRS	= src \
	  lib \
	  scripts \
	  tests

EXTRA_DIST = README.ja \
	     ChangeLog

release: dist
	cp $(distdir).tar.gz /home/shugo/www/shugo.net/babel/
	cd /home/shugo/www/shugo.net/ && make install

ChangeLog: FORCE-ChangeLog
	svn update
	svn log -v > ChangeLog

FORCE-ChangeLog:
	rm -f ChangeLog
