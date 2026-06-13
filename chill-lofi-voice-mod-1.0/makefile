# Chill Voice Mod - Build System
# Requires: .NET SDK (dotnet)
#
# Usage:
#   make                  - Build the plugin DLL
#   make GAME_ROOT="path" - Build with custom game path
#   make install          - Build and copy bundle to game plugin folder

GAME_ROOT ?= E:\SteamLibrary\steamapps\common\Chill with You Lo-Fi Story

CONFIGURATION = Release
PROJECT = src/Cavi.ChillVoiceMod.csproj

.PHONY: all build clean install install-bundle

all: build

check-path:
	@if [ ! -d "$(GAME_ROOT)" ]; then \
		echo "ERROR: Game directory not found: $(GAME_ROOT)"; \
		echo "Specify with: make GAME_ROOT='/your/game/path'"; \
		exit 1; \
	fi
	@echo "Game path: $(GAME_ROOT)"

build: check-path
	dotnet build $(PROJECT) -c $(CONFIGURATION) /p:GamePath="$(GAME_ROOT)"
	@echo ""
	@echo "Build complete! DLL installed to BepInEx/plugins/ChillVoiceMod/"

install-bundle:
	@echo "Copying voice bundle to game plugin folder..."
	@if [ ! -f "bundle/voice_assets_all" ]; then \
		echo "ERROR: bundle/voice_assets_all not found!"; \
		echo "First build the bundle in Unity using the VoiceModToolkit,"; \
		echo "then copy the output file to bundle/voice_assets_all"; \
		exit 1; \
	fi
	@mkdir -p "$(GAME_ROOT)/BepInEx/plugins/ChillVoiceMod"
	cp bundle/voice_assets_all "$(GAME_ROOT)/BepInEx/plugins/ChillVoiceMod/"
	@echo "Bundle installed!"

install: build install-bundle
	@echo ""
	@echo "========================================="
	@echo "  Chill Voice Mod installed!"
	@echo "  Launch the game to test."
	@echo "========================================="

clean:
	rm -rf src/bin/ src/obj/
