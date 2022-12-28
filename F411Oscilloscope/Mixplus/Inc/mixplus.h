
#ifndef __H_MIXPLUS
#define __H_MIXPLUS


#include "main.h"

#ifdef __cplusplus
extern "C" {
#endif /* __cplusplus */

	void setup();
	void loop();
    void dataProc(uint8_t*data, uint32_t Len);

#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif