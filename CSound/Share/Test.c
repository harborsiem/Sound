#include <windows.h>
#include "Utilities.h"
#include <string.h>

typedef void (__stdcall *T_Test_Test)(UBYTE* data, INT32 dataLength, float f1, char* chString);

typedef struct tag_PortControlCreator {
	T_Test_Test netFctTest;
} PortControlCreator;


void other(PortControlCreator* creator) {
	UBYTE* jData;
	char* chString = "unknown";
	jData = (UBYTE*)malloc(20);
	if (jData != NULL) {
		for (UBYTE i = 0; i < 20; i++)
			*(jData + i) = i;
	}
	chString = ((char*)2);

	(creator->netFctTest)(jData, 20, 1.1f, chString);
	free(jData);
}

DllExport void __stdcall Test_Test(
    float f1,
    T_Test_Test netFct)
{
	PortControlCreator creator;
	UBYTE* jData;
	creator.netFctTest = netFct;
	char* chString = "unknown";
	jData = (UBYTE*)malloc(20);
	if (jData != NULL){
	for (UBYTE i = 0; i < 20; i++)
	  *(jData + i) = i;
	}
	chString = ((char*)1);
	(*netFct)(jData, 20, f1, chString);
	free(jData);
	other(&creator);
}
