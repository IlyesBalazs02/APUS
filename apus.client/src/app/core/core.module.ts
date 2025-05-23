// src/app/core/core.module.ts
import { NgModule, Optional, SkipSelf } from '@angular/core';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AuthService } from './services/auth.service';
import { AuthGuard } from './guards/auth.guard';
import { JwtInterceptor } from './interceptors/jwt.interceptor';

@NgModule({
    imports: [
        HttpClientModule
    ],
    providers: [
        AuthService,

        AuthGuard,

        {
            provide: HTTP_INTERCEPTORS,
            useClass: JwtInterceptor,
            multi: true
        }

    ]
})
export class CoreModule {
    // Prevent re-import of the CoreModule
    constructor(@Optional() @SkipSelf() parentModule: CoreModule) {
        if (parentModule) {
            throw new Error(
                'CoreModule is already loaded. Import CoreModule in the AppModule only.'
            );
        }
    }
}
