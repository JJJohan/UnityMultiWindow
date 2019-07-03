#include "UnityInterface.h"
#include "IUnityGraphics.h"
#include "Window.h"
#include <vector>
#include <algorithm>

static MessageFunction _messageDelegate = nullptr;
static IUnityInterfaces* _pUnityInterfaces = nullptr;
static IUnityGraphics* _pGraphicsApi = nullptr;
static UnityGfxRenderer _deviceType = kUnityGfxRendererNull;
std::vector<Window*> _windows;

HGLRC _unityContext = nullptr;
HDC _unityDevice = nullptr;

void Log(std::string message)
{
	if (_messageDelegate == nullptr)
	{
		return;
	}

	_messageDelegate(message.c_str());
}

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
				_unityDevice = wglGetCurrentDC();

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

	void InitPlugin(MessageFunction messageDelegate, CloseFunction closeDelegate, ResizeFunction resizeDelegate)
	{
		_messageDelegate = messageDelegate;
		Window::CloseDelegate = closeDelegate;
		Window::ResizeDelegate = resizeDelegate;

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

	void UpdateWindows()
	{
		for (auto it = _windows.begin(); it != _windows.end(); ++it)
		{
			Window* window = *it;
			if (!window->Render())
			{
				return;
			}
		}
	}
		
	Window* CreateNewWindow(const char* title, int width, int height, bool resizable, unsigned int textureHandle)
	{
		Window* window = new Window(std::string(title), _unityContext, _unityDevice, width, height, resizable, textureHandle);
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

		_windows.erase(std::remove(_windows.begin(), _windows.end(), window), _windows.end());
		delete window;
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