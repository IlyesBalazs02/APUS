import { ActivatedRouteSnapshot, ResolveFn, Router } from "@angular/router";
import { GroupDto } from "./groupsDTOs";
import { inject } from "@angular/core";
import { GroupService } from "./groupService";
import { catchError, of } from "rxjs";

export const groupResolver: ResolveFn<GroupDto | null> = (route: ActivatedRouteSnapshot) => {
    const api = inject(GroupService);
    const router = inject(Router);
    const id = Number(route.paramMap.get('id'));

    return api.get(id).pipe(
        catchError(() => {
            router.navigate(['/groups']);
            return of(null);
        })
    );
};