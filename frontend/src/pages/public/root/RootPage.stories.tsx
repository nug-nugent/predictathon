import type { Meta, StoryObj } from "@storybook/react-vite";
import { RootPage } from "./RootPage";

const meta = {
    title: "Pages/Public/Root",
    component: RootPage
} satisfies Meta<typeof RootPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Root: Story = {};
