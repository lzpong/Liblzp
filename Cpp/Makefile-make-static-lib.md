### 一劳永逸,仅需改动两个参数`MYLIB`和`VPATH`即可生成 静态库(.a) 的 Makefile 文件

```makefile
PWD=$(shell pwd)
#INCS=-I$(PWD)/include

# change to you project name
MYLIB = SQLiteCpp.a
# change to you project file dir
VPATH = include:src:sqlite3
# the obj dir
OBJDIR = obj

###########################################################################
# auth lzpong
# source files
SRCS = $(foreach dir,$(subst :, ,$(VPATH)),$(wildcard $(dir)/*.cpp))
SRCSC = $(foreach dir,$(subst :, ,$(VPATH)),$(wildcard $(dir)/*.c))
# obj files
OBJS_1 = $(addsuffix .o,$(basename $(SRCS)))
OBJSC_1 = $(addsuffix .o,$(basename $(SRCSC)))
OBJS = $(foreach n,$(notdir $(OBJS_1)),$(OBJDIR)/$(n))
OBJSC = $(foreach n,$(notdir $(OBJSC_1)),$(OBJDIR)/$(n))
# head files
HEADERS = $(foreach dir,$(subst :, ,$(VPATH)),$(wildcard $(dir)/*.h))
HEADERS += $(foreach dir,$(subst :, ,$(VPATH)),$(wildcard $(dir)/*.hpp))
HEADERS += $(foreach dir,$(subst :, ,$(VPATH)),$(wildcard $(dir)/*.inc))

CC = gcc
CXX = g++ -std=c++11
INCS = $(patsubst %,-I%,$(subst :, ,$(VPATH)))
CFLAGS += $(INCS)
CXXFLAGS += $(INCS)

LIBS += -lncurses -lesl -lpthread -lm
LDFLAGS += -L.
PICKY = -O2
#SOLINK = -shared -Xlinker -x

#DEBUG = -g -ggdb
#LIBEDIT_DIR = ./


all: $(MYLIB)

$(MYLIB): $(OBJS) $(SRCS) $(OBJSC) $(SRCSC) $(HEADERS)
    ar rcs $(MYLIB) $(OBJS) $(OBJSC)
    ranlib $(MYLIB)

# *.cpp files commpare
$(OBJS): $(SRCS) $(HEADERS)
    @test -d $(OBJDIR) | mkdir -p $(OBJDIR)
    $(CXX) -c $(SRCS) $(INCS)
    mv *.o $(OBJDIR)/

# *.c file commpare
$(OBJSC): $(SRCSC) $(HEADERS)
    @test -d $(OBJDIR) | mkdir -p $(OBJDIR)
    $(CC) -c $(SRCSC) $(INCS)
    mv *.o $(OBJDIR)/


clean:
    rm -rf $(OBJDIR)
    rm -f *.o *.a

```
