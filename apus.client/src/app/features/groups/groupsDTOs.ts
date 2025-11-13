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

