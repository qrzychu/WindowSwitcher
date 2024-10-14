TARGET = VirtualDesktopManager.dll
SRC = VirtualDesktopManager.cpp

all: $(TARGET)

$(TARGET): $(SRC)
	clang++ -shared -lole32 -loleaut32 -o $(TARGET) $(SRC)

clean:
	del /Q $(TARGET) *.exp *.lib *.o *.obj

.PHONY: all

# Debugging information
debug:
	@echo Current directory: $(CURDIR)
	@echo Source file: $(SRC)
	@echo Target: $(TARGET)
	@dir

.PHONY: debug