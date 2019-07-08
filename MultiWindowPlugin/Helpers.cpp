#include "Helpers.h"
#include "UnityInterface.h"
#include <sstream>
#include <GL/glew.h>
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

std::string GetLastErrorAsString(unsigned long errorMessageID)
{
	LPSTR messageBuffer = nullptr;
	const size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		nullptr, errorMessageID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), LPSTR(&messageBuffer), 0, nullptr);

	std::string message(messageBuffer, size);

	//Free the buffer.
	LocalFree(messageBuffer);

	return message;
}

void ErrorCheck(const std::string& event)
{
	GLenum err;
	while ((err = glGetError()) != 0) {
		std::stringstream ss;
		ss << "OpenGL Error: ";
		ss << event;
		ss << " - ";
		ss << err;
		Log(ss.str());
	}
	   
	const DWORD t = GetLastError();
	if (t != 0)
	{
		std::stringstream ss;
		ss << "Error: " << t << " - " << GetLastErrorAsString(t);
		Log(ss.str());
	}
}