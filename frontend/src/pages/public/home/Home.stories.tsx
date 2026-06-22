import type { Meta, StoryObj } from "@storybook/react-vite";
import { HomePage } from "./Home";
import { nug, stu } from "../../../services/user-service";

const meta = {
    title: "Pages/Home",
    component: HomePage
} satisfies Meta<typeof HomePage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const NotLoggedIn: Story = {};

export const LoggedInAsStu: Story = {
    parameters: { user: stu }
};

export const LoggedInAsNug: Story = {
    parameters: { user: nug }
};
