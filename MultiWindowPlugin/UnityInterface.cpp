#include "UnityInterface.h"
#include "IUnityGraphics.h"
#include "Window.h"
#include <vector>
#include <algorithm>

MessageFunction _messageDelegate = nullptr;
IUnityInterfaces* _pUnityInterfaces = nullptr;
IUnityGraphics* _pGraphicsApi = nullptr;
UnityGfxRenderer _deviceType = kUnityGfxRendererNull;
HGLRC _unityContext = nullptr;
std::vector<Window*> _windows;

void Log(const std::string& message)
{
	if (_messageDelegate == nullptr)
	{
		return;
	}

	_messageDelegate(message.c_str());
}

#ifdef _WIN32
HWND _pUnityWindowHandle = nullptr;

BOOL EnumWindowsProc(HWND hWnd, LPARAM lParam)
{
	char className[32];
	GetClassName(hWnd, className, 32);
	if (std::string(className) == "UnityWndClass")
	{
		_pUnityWindowHandle = hWnd;
		return false;
	}
	return true;
}

HWND GetUnityWindowHandle()
{
	if (_pUnityWindowHandle == nullptr)
	{
		const unsigned int threadId = GetCurrentThreadId();
		EnumThreadWindows(threadId, EnumWindowsProc, 0);
	}

	return _pUnityWindowHandle;
}
#endif

extern "C"
{
	static void OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
	{
		// Create graphics API implementation upon initialization
		if (eventType == kUnityGfxDeviceEventInitialize)
		{
			_deviceType = _pGraphicsApi->GetRenderer();
			if (_deviceType == kUnityGfxRendererOpenGLCore)
			{
				_unityContext = wglGetCurrentContext();

				glewExperimental = GL_TRUE;
				if (glewInit() != GLEW_OK)
				{
					// TODO: Log glew init failure.
				}

				Window::LoadResources();
			}
		}
		
		// Cleanup graphics API implementation upon shutdown
		if (eventType == kUnityGfxDeviceEventShutdown)
		{
			if (_deviceType == kUnityGfxRendererOpenGLCore)
			{
				Window::UnloadResources();
			}
			
			_deviceType = kUnityGfxRendererNull;
		}
	}
	
	void UnityPluginLoad(IUnityInterfaces* unityInterfaces) 
	{
		_pUnityInterfaces = unityInterfaces;
		_pGraphicsApi = unityInterfaces->Get<IUnityGraphics>();
		_pGraphicsApi->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	void UnityPluginUnload()
	{
		_pGraphicsApi->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
		_pGraphicsApi = nullptr;
	}

	void InitPlugin(
		MessageFunction messageDelegate, 
		CloseFunction closeDelegate, 
		ResizeFunction resizeDelegate, 
		MouseUpdateFuncton mouseDelegate,
		MoveFunction moveDelegate
	)
	{
		_messageDelegate = messageDelegate;
		Window::CloseDelegate = closeDelegate;
		Window::ResizeDelegate = resizeDelegate;
		Window::MouseDelegate = mouseDelegate;
		Window::MoveDelegate = moveDelegate;

		if (SDL_Init(SDL_INIT_VIDEO) < 0)
		{
			Log("SDL could not initialise!");
			return;
		}

		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 4);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 5);
		SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, 16);
		SDL_GL_SetAttribute(SDL_GL_STENCIL_SIZE, 8);
		SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
		SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
		SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
		SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 0);
		SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 0);
	}

	void ForwardWindowEvent(const SDL_Event& event)
	{
		Window* targetWindow = nullptr;
		for (auto it = _windows.begin(); it != _windows.end(); ++it)
		{
			Window* window = *it;
			if (window->ID == event.window.windowID)
			{
				targetWindow = window;
				break;
			}
		}

		if (targetWindow != nullptr)
		{
			targetWindow->HandleEvent(event);
		}
	}

	void UpdateWindows()
	{
		SDL_Event event;
		while (SDL_PollEvent(&event) != 0)
		{
			if (event.type == SDL_WINDOWEVENT)
			{
				ForwardWindowEvent(event);
			}
		}

		for (auto it = _windows.begin(); it != _windows.end(); ++it)
		{
			Window* window = *it;
			window->Render();
		}
	}
		
	Window* CreateNewWindow(const char* title, int width, int height, bool resizable, unsigned int textureHandle)
	{
		Window* window = new Window(std::string(title), _unityContext, width, height, resizable, textureHandle);
		if (!window->CreateContext())
		{
			delete window;
			return nullptr;
		}

		_windows.push_back(window);
		return window;
	}

	void DisposeWindow(Window* window)
	{
		if (window == nullptr)
		{
			return;
		}

		const auto windowIndex = std::find(_windows.begin(), _windows.end(), window);
		if (windowIndex != _windows.end())
		{
			_windows.erase(windowIndex);
		}

		delete window;
	}

	void SetWindowPosition(Window* windowHandle, int x, int y)
	{
		if (windowHandle == nullptr)
		{
			return;
		}

		windowHandle->SetPosition(x, y);
	}

	void DragWindow(Window* windowHandle)
	{
		if (windowHandle == nullptr)
		{
			return;
		}

		windowHandle->Drag();
	}

	void ShutdownPlugin()
	{
		for (auto it = _windows.begin(); it != _windows.end(); ++it)
		{
			delete *it;
		}
		_windows.clear();

		SDL_Quit();
	}
}