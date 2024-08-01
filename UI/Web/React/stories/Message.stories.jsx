import React from 'react';
import { Message } from '../Components/Message/Message';

export default {
    title: "Messageboard Thread/Message Component",
    component: Message,
    parameters: {
        layout: "fullscreen",
    },
    argTypes: {
        onOpenEmojiPicker: { description: "Called when the Add Reaction button is pressed", action: "Add reaction button clicked"},
        onAddReaction: { description: "Called when adding a reaction", action: "Add reaction"},
        onRemoveReaction: { description: "Called when removing a reaction", action: "Remove reaction"},
    }
}

const Template = (args) => <Message {...args} />;
const BaseArgs = {
    id: "a-post",
    authorImageUrl: "https://www.predictathon.co.uk/Uploads/Images/13473b15-dc61-437c-833a-af2b987b67ef_sm.jpg",
    authorUrl: "https://www.predictathon.co.uk/Pages/User/UserDetail.aspx?UserID=13473b15-dc61-437c-833a-af2b987b67ef",
    authorName: "stu with a long username",
    date: new Date(),
    text: "Here is some **message** text.\n\nParagraph 2 includes a URL like https://www.google.com.\n\nAnd here's a third paragraph.",
    reactions: [{"Name":"rolling on the floor laughing","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f923.svg","Username":"stu with a long username","IsMe":false},{"Name":"brewdog","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png","Username":"stu with a long username","IsMe":false},{"Name":"brewdog","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/brewdog.png","Username":"Nugsson","IsMe":false},{"Name":"guinness","Url":"https://www.predictathon.co.uk/Images/Message/Reactions/pt.png","Username":"Nugsson","IsMe":false},{"Name":"beach with umbrella","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f3d6.svg","Username":"Nugsson","IsMe":false},{"Name":"safety vest","Url":"https://cdnjs.cloudflare.com/ajax/libs/twemoji/13.0.0/svg/1f9ba.svg","Username":"3rd User","IsMe":true}]
};

export const Desktop = Template.bind({});
Desktop.args = BaseArgs;

export const Mobile = Template.bind({});
Mobile.args = BaseArgs;
Mobile.parameters = { viewport: { defaultViewport: 'pixel5' } };
