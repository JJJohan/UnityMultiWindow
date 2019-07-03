#pragma once

#include <string>

std::string GetLastErrorAsString(unsigned long errorMessageID);
void ErrorCheck(std::string event);