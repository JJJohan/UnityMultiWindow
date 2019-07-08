#pragma once

#include <string>

#define DllExport __declspec(dllexport)

class Window;

typedef void (__stdcall *MessageFunction)(const char* message);
typedef void (__stdcall *CloseFunction)(Window* window);
typedef unsigned int (__stdcall *ResizeFunction)(Window* window, int width, int height);
typedef void(__stdcall *MouseUpdateFuncton)(Window* window, int mouseX, int mouseY, unsigned int buttonMask);
typedef void(__stdcall* MoveFunction)(Window* window, int mouseX, int mouseY, bool insideUnityWindow);

extern "C"
{
	DllExport void InitPlugin(MessageFunction messageDelegate, CloseFunction closeDelegate, ResizeFunction resizeDelegate, MouseUpdateFuncton mouseDelegate, MoveFunction moveDelegate);
	DllExport void ShutdownPlugin();

	DllExport Window* CreateNewWindow(const char* title, int width, int height, bool resizeable, unsigned int textureHandle);
	DllExport void UpdateWindows();
	DllExport void DisposeWindow(Window* windowHandle);
	DllExport void SetWindowPosition(Window* windowHandle, int x, int y);
	DllExport void DragWindow(Window* windowHandle);
}

void Log(const std::string& message);

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
HWND GetUnityWindowHandle();
#endif