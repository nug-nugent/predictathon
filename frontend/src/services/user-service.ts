import { UserRole, type User } from "../providers/UserProvider";

export const stu: User = {
    name: "Stu has a really long username",
    role: UserRole.User,
    avatarUrl: "https://www.predictathon.co.uk/Uploads/Images/13473b15-dc61-437c-833a-af2b987b67ef_sm.jpg",
    currentCompetition: "Euro 2024"
};
export const nug: User = {
    name: "Nugsson",
    role: UserRole.Admin,
    avatarUrl: "https://www.predictathon.co.uk/Uploads/Images/da93a123-baae-4ca4-9874-aad53feac685_sm.jpg",
    currentCompetition: "World Cup 2026"
};

export async function loginUser(username: string): Promise<User> {
    await new Promise(resolve => setTimeout(resolve, 500));

    return username.toLocaleLowerCase() === "nug" ? nug : stu;
}