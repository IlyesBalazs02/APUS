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
}

export interface DecideJoinRequestDto {
    approve: boolean;
}
