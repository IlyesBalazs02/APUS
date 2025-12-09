import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { CreateGroupDto, CreateGroupEventDto, CreateGroupPostDto, DecideJoinRequestDto, GroupDto, GroupEventDto, GroupEventParticipantDto, GroupJoinRequestDto, GroupMembersDto, GroupPostDto, GroupSettingsDto, UpdateGroupDto, UpdateGroupSettingsDto } from "./groupsDTOs";
import { firstValueFrom } from "rxjs";
import { PagedResponse } from "../../shared/DTOs/PagedResponse";

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

    getRequests(groupId: number) {
        return this.http.get<GroupJoinRequestDto[]>(`${this.base}/${groupId}/requests`);
    }

    //#region  settings

    getSettings(groupId: number) {
        return this.http.get<GroupSettingsDto>(`${this.base}/${groupId}/settings`);
    }

    updateSettings(groupId: number, dto: UpdateGroupSettingsDto) {
        return this.http.patch<void>(`${this.base}/${groupId}/settings`, dto);
    }

    //#endregion

    //#region Posts
    getPosts(groupId: number, skip: number, take: number) {
        let params = new HttpParams().set('skip', skip).set('take', take);
        return this.http.get<PagedResponse<GroupPostDto>>(`${this.base}/${groupId}/posts`, { params });
    }

    createPost(groupId: number, dto: CreateGroupPostDto) {
        return this.http.post<GroupPostDto>(`${this.base}/${groupId}/posts`, dto);
    }

    deletePost(postId: number) {
        // matches DELETE api/groups/posts/{postId}
        return this.http.delete<void>(`${this.base}/posts/${postId}`);
    }
    //#endregion


    //#region events
    getEvents(groupId: number, skip: number, take: number) {
        const params = new HttpParams()
            .set('skip', skip)
            .set('take', take);

        return this.http.get<PagedResponse<GroupEventDto>>(
            `${this.base}/${groupId}/events`,
            { params }
        );
    }

    createEvent(groupId: number, dto: CreateGroupEventDto) {
        return this.http.post<GroupEventDto>(`/api/groups/${groupId}/events`, dto);
    }

    deleteEvent(eventId: number) {
        return this.http.delete<void>(
            `${this.base}/events/${eventId}`
        );
    }

    getEventParticipants(eventId: number) {
        return this.http.get<GroupEventParticipantDto[]>(
            `/api/groups/events/${eventId}/participants`
        );
    }

    joinEvent(groupId: number, eventId: number) {
        return this.http.post<void>(
            `/api/groups/${groupId}/events/${eventId}/participants`,
            {}
        );
    }

    leaveEvent(groupId: number, eventId: number) {
        return this.http.delete<void>(
            `/api/groups/${groupId}/events/${eventId}/participants`
        );
    }

    //#endregion

}