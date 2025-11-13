export interface CreateGroupDto {
    name: string;
    description?: string | null;
    isOpen: boolean;
}

export interface UpdateGroupDto {
    name?: string;
    description?: string | null;
    isOpen?: boolean;
}

export interface GroupDto {
    id: number;
    name: string;
    description?: string | null;
    isOpen: boolean;
    createdByUserId: string;
    createdAtUtc: string;
    memberCount: number;

    isMember: boolean;
    isAdmin: boolean;

    hasPendingJoinRequest: boolean;
}

export interface DecideJoinRequestDto {
    approve: boolean;
}

export interface GroupMembersDto {
    userId: string;
    fullName: string;
    avatarUrl: string;
    role: string;
    joinedAtUtc: string;
}

export interface GroupJoinRequestDto {
    id: number;
    requesterUserId: string;
    fullName: string;
    avatarUrl: string | null;
    requestedAtUtc: string;
}

//#region settings
export enum GroupPostPermission {
    AdminsOnly = 0,
    Members = 1
}

export enum GroupEventPermission {
    AdminsOnly = 0,
    Members = 1
}

export interface GroupSettingsDto {
    groupId: number;
    name: string;
    description?: string | null;
    isOpen: boolean;
    whoCanPost: GroupPostPermission;
    whoCanCreateEvent: GroupEventPermission;
}

export interface UpdateGroupSettingsDto {
    name?: string;
    description?: string | null;
    isOpen?: boolean;
    whoCanPost?: GroupPostPermission;
    whoCanCreateEvent?: GroupEventPermission;
}
//#endregion settings

