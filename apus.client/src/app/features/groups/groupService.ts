import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { CreateGroupDto, DecideJoinRequestDto, GroupDto, GroupMembersDto, UpdateGroupDto } from "./groupsDTOs";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class GroupService {
    private base = '/api/groups';

    constructor(private http: HttpClient) { }

    create(dto: CreateGroupDto) {
        return this.http.post<GroupDto>(this.base, dto);
    }

    get(id: number) {
        return this.http.get<GroupDto>(`${this.base}/${id}`);
    }

    search(q: string | null, skip: number, take: number) {
        let params = new HttpParams().set('skip', skip).set('take', take);
        if (q && q.trim()) params = params.set('q', q.trim());
        return this.http.get<GroupDto[]>(this.base, { params });
    }

    join(groupId: number) {
        return this.http.post<void>(`${this.base}/${groupId}/join`, {});
    }

    leave(groupId: number) {
        return this.http.post<void>(`${this.base}/${groupId}/leave`, {});
    }

    update(groupId: number, dto: UpdateGroupDto) {
        return this.http.patch<void>(`${this.base}/${groupId}`, dto);
    }

    decide(requestId: number, approve: boolean) {
        const dto: DecideJoinRequestDto = { approve };
        return this.http.post<void>(`${this.base}/requests/${requestId}/decide`, dto);
    }

    async getOnce(id: number) { return firstValueFrom(this.get(id)); }

    getMembers(groupId: number) {
        return this.http.get<GroupMembersDto[]>(`${this.base}/${groupId}/members`);
    }

    kickMember(groupId: number, userId: string) {
        return this.http.delete<void>(`${this.base}/${groupId}/members/${userId}`);
    }
}