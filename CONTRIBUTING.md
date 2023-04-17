**Note**: Creating guidelines for contributing to Project-Origin is a work in progress, and this document may be subject to substantial changes. However, feel free to use these contribution guidelines as a starting point/template in your own project, when creating structures and guidelines for contributions. 

# Contribution Guidelines

## Table of Contents

1. [Introduction](#introduction)
2. [General Community Guidelines](#general-community-guidelines)
    - [Two-Factor Authentication (2FA)](#two-factor-authentication-2fa)
    - [Labels](#labels) 
4. [Choosing between Contribution Options](#choosing-between-contribution-options)
5. [Issues](#issues)
    - [Task](#task)
    - [Feature Request](#feature-request)
    - [Bug Report](#bug-report)
6. [Pull Requests](#pull-requests)
    - [Creating a Pull Request](#creating-a-pull-request)
    - [Pull Request Templates](#pull-request-templates)
    - [Reviewing Pull Requests](#reviewing-pull-requests)
7. [Project Boards](#project-boards)
    - [Creating a New Project Board](#creating-a-new-project-board)
    - [Using the Backlog Project Boards](#using-the-backlog-project-boards)
    - [Draft issues](#draft-issues)
8. [Discussions](#discussions)
    - [Creating a Discussion](#creating-a-discussion)
    - [General](#general)
    - [Announcements](#announcements)
    - [Ideas](#ideas)
    - [Polls](#polls)
    - [Q & A](#q--a)
    - [Show and Tell](#show-and-tell)


## Introduction

Thank you for considering contributing to our project! 
The purpose of this document is to create a common framework, clearly specifying expected behavior when communicating and interacting within this community. 
Hopefully, this document can make the contributing process more clear and answer some questions that you may have.


Given that this is a dynamic, evolving project, these guidelines may change over time. 
If you have a specific suggestion for modifying or reformulating something in this guideline, feel free to [create a Pull Request], containing your suggestion. 

There are some general rules that apply to everyone who wants to contribute to this repository. 
These guidelines will be treated in the first part of this document. 

More specifically, contributions are welcome in the form of [discussion posts](#discussions), [issues](#issues), and [pull requests](#pull-requests). 
The guidelines for each of these communication forms will be described in the following. 


## General Community Guidelines

### Two-Factor Authentication (2FA)
Participants can only be allowed access to the Project-Origin organization, if they have two-factor authentication activated for their Github account. More information on how to activate two-factor authentication [here](https://docs.github.com/en/authentication/securing-your-account-with-two-factor-authentication-2fa/configuring-two-factor-authentication).

### Labels
Labels are useful to inform other community members about the topic(s) treated in your issues and pull requests. Further, it allows community members to find any pull requests and issues relevant to them, by using labels for filtering. 

The labels are divided into categories, as provided below, along with an individual description of each label:

- **Green**: Project governance and collaboration related stuff. Includes labels `governance`, `legal`, `communication` and `collaboration`.
  - `governance`: Setting up project governance structures or rules
  - `legal`: Deals with legal apects of the project
  - `communication`: Treats communications-related topics
  - `collaboration`: Related to setting up collaboration structures
- **Blue**: Product related stuff. Includes labels `documentation`, `product definition` and `enhancement`. 
  - `documentation`: Improvements or additions to documentation
  - `product definition`: Related to our work in defining the individual parts that make up Project-Origin
  - `enhancement`: New feature or request
- **Purple**: Everything that requires/has required inputs for help. Includes labels `question`, `help wanted`, `urgent` and `good first issue`.
  - `question`: Further information is requested
  - `help wanted`: Extra attention is needed
  - `good first issue`: Good for newcomers
  - `urgent`: This needs to be addressed asap 
- **Red**: Everything that isn't working, isn't right or won't be fixed. Includes labels `wont fix`, `bug` and `invalid`.
  - `wont fix`: This will not be worked on
  - `bug`: Something isn't working
  - `invalid`: This does not follow the contribution guidelines
- **Gray**: The more meta and maintaining/structuring kind of stuff. Includes labels `planning`, `todo`, `meta`, `housekeeping` and `duplicate`.
  - `planning`: For the purpose of planning
  - `todo`: This is a task that needs to be done 
  - `workingsession-notes`: To be assigned on all working session notes pull requests so they can easily be found.
  - `meta`: Meta content on GitHub org, Organisation etc.
  - `housekeeping`: Things don't stay tidy themselves. Deals with all things keeping things tidy and neat.
  - `duplicate`: This issue or pull request already exists



## Choosing between Contribution Options
The most appropriate choice of contribution form, depends on, for one thing, how specific your contribution is. Some general guidelines for when to choose which are:

- Use the [Discussions], when one of these situations apply to you
  - You don't know where to find something, or how to carry out a certain task on Github
  - You are considering several alternatives for a solution, and it is not clear which one is best
  - You have a loose idea for something, but need inputs for it to become an actual feature to request
  - You have some information that you think is relevant for the community, about related news, something happening, or something you and/or others made in this project.
- Use the [Issues], when one of these situations apply to you
  - You have a specific task/list of tasks related to the project, that you want to track, while potentially receiving inputs from the community
  - You have a specific idea for a new feature, but you don't know how to implement it yet, or you want to refine the idea with the Trusted Committer and community before creating a pull request
  - You discovered a bug, and need inputs from the community and trusted committer, before knowing how to fix the bug
- Use the [Pull Request]s if any of these cases apply:
  - You want to create a new file
  - You want to add something to an existing file
  - You want to modify something specific in an existing file
- Use [Projects] when you:
  - Want an overview of issues treated by the Registry working group and their status
  - Want to quickly go through issues currently worked on, and add comments to one or several of them


## Issues 
Everyone in the community can create issues, using one of the [Issue Templates] provided, or add comments and suggestions to others' issues posted here in this repository. 
Issues are meant for discussing specific, closed-form ideas or details of a project. 

To create an issue in this repository, navigate to the "Issues" tab within the repository, and click on the green button in the upper right corner, "New Issue", or click here: [Issue Templates]. 
Then, a number of issue templates to choose from appears:

<img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/contribution_issue_templates.png" width="90%"/>

In Github, there are some helping features that apply to all issues, shown and described in the following table.

| Feature       | Description                                                    | 
| ------------- | -------------------------------------------------------------- |
| `Assignees`   | Type or choose a user to be responsible for the issue          |
| `Labels`      | Apply any of the available labels relevant to the issue        |
| `Projects`    | Select a project to contain the issue card if relevant         |
| `Milestone`   | If relevant, select an open Milestone for the issue            |
| `Development` | Link any pull request containing content related to this issue |


In this repository, it is expected that the contributor at least provides information in the sections "Assignee" and "Labels". A list of the labels used in this repository is provided in the [Labels section](#labels).

In the following, some information will be provided to help you decide, which issue template best suits what you want to communicate. 


### Task 
The Task issue informs the community of a task that you, as a contributor, are working on. 
It helps the community keep up to date with your progress, and creates a space for you to receive inputs and feedback on your task. 

#### _Format_

_Title_:
Leave the "✏️ [TASK]" part of the title, and fill in an concise, but sufficiently informative title at "<title>" to describe the task.

_Start Date_: 
When did/will you start working on this task? Fill in this field with the Day/Month/Year. 

_Description_:
Briefly describe the task you are working on in this field. 

_Motivation_:
Describe why this task needs to be done. Which value does the task provide to the project or community? 

_Definition of Done_:
Describe what needs to happen before the task can be considered done. Some examples on what to provide here is:

- Do you need specific inputs from someone/the community? 
- Does a certain number of steps need to be completed? 
- Are changes to certain access rights necessary? 

_Additional Information_:
Add something here, if you have some background information you want people to know, or if there are related issues or pull requests related to the task. 


### Feature Request
The Feature Request issue is used for specific ideas and improvement suggestions. 
Everyone from the community can provide inputs to the feature request.
In the spirit of Open Source and InnerSource the focus lies on you as a contributor in doing (most of) the work that you have requested yourself with the supervision and some guidance of the [Trusted Committer]s once there is enough agreement on the feature.

#### _Format_ 

_Title_: 
Leave the "💡 [REQUEST]" part of the title, and fill in a concise, but sufficiently informative title at "<title>" that describes the feature you want to implement. 

_Start Date_: 
When did/will you start working on this feature request? Fill in this field with the Day/Month/Year. 

_Implementation PR_: 
If you have started implementing something on Github, add the relevant pull request here.

_Reference issues_: 
If other issues in the repository are related to your feature request, you should link it here. 

_Summary_: 
Provide a brief explanation of what the feature you want to implement is about.

_Basic Example_: 
Fill in an example of a situation, where your feature will be useful.

_Drawbacks_: 
Fill in any negative impacts caused by your feature in this field. 

_Unresolved Questions_: 
If there are some elements related to your feature that are still unclear, you should describe those elements here. Additionally, you should add the label `help wanted` to your issue to let the community know that something needs inputs and/or processing. 



### Bug Report
A bug report is used to inform the community of an error or problem encountered in the project. It allows the community to discuss the problem, and find potential solutions. It is customary to directly offer your fix for the problem as a pull request for review to facilitate and speed up the solution of the problem. Even if not perfect, this can also advance the discussion by discussing it more specifically.

#### _Format_ 

_Title_: 
Leave the "🐞 [BUG]" part of the title, and fill in a concise line describing the bug at "<title>".

_To Reproduce_:
Write down a step-by-step walkthrough of how you found/encountered the error, including a description of the error, in the last step.

_Expected Behavior_:
Describe what you expected to happen instead of the error you encountered.

_Screenshots_: 
If applicable, you can add screenshots of the error encountered here. 

_Desktop_: 
In this field, you should write down your computers operating system (e.g., iOS, Windows), the browser you used (e.g., Safari, Edge) and the version of your operating system and browser. If the bug does not involve access through a browser, you can state "not applicable" if there is none.

_Additional Context_:
Of there is any additional, relevant information about the bug you encountered, you can add it here. 
 

## Pull Requests
Everyone in the community can create a Pull Request, or add reviews, comments, and suggestions to existing [Pull Request]s. 

### Creating a Pull Request
If you want to create a new file, or make changes to an existing file, you must create a pull request by either:

1. Navigating to a relevant location for your new file, and click on "Add file" and then "Create new file" in the drop-down menu. Give the file a title (remember to add ".md" if you want to create a Markdown file), and fill in the file with your content. 

  <img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/contribution_create_new_file.png" width="30%"/>

2. Navigating to the [Pull Request Templates] and find a relevant template for your pull request. This is relevant if you want to create 1. a Request for Comments ([RFC]), or 2. a [Meeting Minutes].

  i. Click on the pull request template you want to use
  ii. Click on the "Copy raw contents" symbol (The two overlapping squares)
  iii. Navigate to a relevant location for your file, click on "Add file" and then "Create new file" in the drop-down menu. 
  iv. Left-click in the new file and choose "paste" in the drop-down menu, or press ctrl+V to paste the template. 
  v. Fill in the template with your content 
 
3. Navigating to the location of the file you want to modify, click on the file, and then click on the "✏️" in the upper right corner of the file. 

   <img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/contribution_edit_file.png" width="90%"/>

#### Commit Message
The commit message must be filled with the following information: 

_Description of the change(s) made_ 
Briefly describe the changes you have made, whether it is a new file proposed, or additions/changes to an existing document. Try to capture the changes in a few short, concise sentences that are easy to grasp, but still describes the changes in a sufficiently specific way. 

_Reason for suggested change(s)_
Describe the problem that the changes mitigate, or the goal that the change helps achieve. 

_How did you implement the change(s)?_
If applicable, provide some information about your methods for creating the new file or changes. 

_Additional information_
Provide any other related and/or relevant information here. This could be some background information, or a related issue, pull request or discussion.

#### Commit Settings
You must always create a new branch for your pull request. 
Hence, when submitting it, select the setting "Create a new branch for this commit and start a pull request". When you create a separate branch, you isolate your work without affecting other branches in the repository.

Potentially edit the branch name, to help the rest of the community easily understand what the branch contains.

#### Best Practices
There are a number of best practices, which will help ensure that the path towards merging your contribution runs smoothly: 

-	Try to use very small and short pull requests that describe the change in an easy-to-grasp way, to increase the chance of quick merging times.
-	Unrelated people will try to understand what your pull request changes. Thus, providing meaningful and brief commit messages allows for meaningful, reasonably quick reviews
- It is expected that the contributor at least provides information in the sections "Assignee" and "Labels". A list of the labels used in this repository is provided [here](#labels)
- If you want someone specific to review your pull request, you can add them in the "Reviewers" section

### Pull Request Templates
This repository has a number of [Pull Request Templates] for documents with certain, often-used structures, to make it easier for participants to create these files. The templates will be described individually below. 

#### Request for Comments (RFC)
Requests for Comments (RFCs) can be submitted to this repository, to make bigger, more extensive change proposals. 
The RFC must contain the following information:

_Title_:
Leave the "📬 \[RFC\] 000" part of the title, and fill in a title at "<title>" that encompasses the suggestion provided in the RFC. The number "000" illustrates the status of the RFC, and means that it has the status "draft". The RFC will change status over time, as it is treated by the community and the [Trusted Committer of this repository]. The RFC can be assigned one of the following statuses:


| Status       | Description                                                     | 
| ------------ | --------------------------------------------------------------- |
| `Draft`      | The RFC is a draft and requires further discussion and/or work  |
| `Proposed`   | The RFC is a proposal to the Project-Origin partnership         |
| `Accepted`   | The RFC is accepted by the Project-Origin partnership           |
| `Rejected`   | The RFC is rejected by the Project-Origin partnership           |
| `Superseded` | The RFC is no longer active, as it is superseded by another RFC |

_Summary_: 
Summarize in 1-2 sentences problem adressed, and the suggested solution.

_Context_:
This field should be filled in with some background information answering:

- Where does the problem exist?
- What are the pre-conditions?

_Problem_: 
Fill in this field with challenges and/or issues that the suggested change addresses. This section should adress:

- What makes the problem difficult?
- What are the trade-offs?

_Rationale_:
This field should contain information that explains _why_ this is the right solution. Optionally, fill in some information about alternative solutions, and why they were discarded. 

_Implications_: 
Describe which implications the proposal will have on Project-Origin, if your proposal is accepted. 

_Additional Information_: 
Add any relevant, additional information in this field. It could be, e.g., examples of known instances of you proposed solution in other business/contexts, or any co-authors of the proposal besides yourself. 

#### Meeting Minutes 
The meeting minutes template is used, when a working group in a Project-Origin project carries out meetings, to make sure that important points from the meeting are recorded and shared with the rest of the community. The meeting minutes contain the following fields: 

_Title_: 
The defalt title is "Project-Origin Working Group Meeting DD, MM, YYYY", which should be modified to contain the meeting date. 

_Roles_: 
To ensure that the meeting runs smoothly, three meeting participants will be assigned with a role beforehand:
- Scheduler
  - The scheduler is responsible for creating the meeting minutes document, prepare an agenda, and send a calendar invite to the rest of the meeting attendees.
- Facilitator
  - The facilitator conducts the meeting and makes sure that all of the agenda items are finished within the allotted time.
- Scribe
  - The scribe writes down key points during the meeting by adding them to the meeting minutes pull request, that the scheduler created. 
  
More information about the three roles is provided [here](https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/meeting_roles.md). 

_Agenda_: 
This part of the document must be filled in with agenda points to go through in the meeting. Some suggested agenda points are added to the template, but can be modified as the scheduler sees fit. 

_Notes_: 
This section is for the scribe to fill in with the points: 
- Attendees
  - Should be filled in with names and Github usernames of the meeting's participants.
- Key points
  - Records main elements discussed in the meeting, and the outcome of the discussions. 
- Assignments 
  - If meeting participants agree on some assignments to do, they can be recorded here.
- Next meeting's roles
  - If the meeting is part of a recurring series of meetings, the participants can agree on someone to fill in the three roles for the next meeting. 


### Reviewing Pull Requests
If you want to review someone else's pull request, you can review changes and leave individual comments or make change suggestions in the file(s) contained in the pull request, by:

- Navigating to the pull request you want to review, from the list of open [Pull Request]s
- In the pull request, click on the tab "Files changed"
- Hover over the line of code where you'd like to add a comment, or suggest a change and click the blue ➕ icon that appears. 
- To add a comment or suggestion for multiple lines, click and drag to select the range of lines, then click the blue comment icon.
  - If you want to make a suggestion, you must click the square with a "+-" sign inside, in the top right corner of the comment box that appears.
- When you are done adding your comment or suggestion, you can either choose "Add Single Comment", if you only have one thing to add, or "Start a Review" if you want to add several comments and/or suggestions.
  

## Project Boards
Everyone in the community can create and participate in using the Project boards in the _origin-collaboration_ repository, found under the [Projects] tab. 
The Project boards are used to give an overview of planned, ongoing or blocked tasks, and to enable easy navigation to and interaction in all of those issues without the need to search for them one-by-one in the full list of [Issues]. 
    
Currently active, open Project boards are: 


- **[Project-Origin Backlog]**: This is an internal backlog for all the "non-software" kind of work in Project Origin. This Project board is relevant to **everyone** participating in the Project-Origin partnership. 
- **[First Workshop in Aarhus 8.11.2022]**: This Project board provides an overview of all the stickie notes produced at the first workshop.

The guidelines for creating and using Project boards are provided in the following.

### Creating a New Project Board
An extensive guide on how to create a new Project board is provided in the [Projects Guide]. When creating a new Project board, consider the following guidelines for this repository:
  
- Give the Project board an informative title and preferably description as well, to make it easier for the community members to understand what the board contains, and whether or not it is relevant to them
- Create board views sorted in ways that are easy to understand, to allow other community members to potentially interact with the board as well. You are welcome to draw inspiration from the existing project boards.
- If you want to create a Project board to keep an overview of your own tasks, you must change its access settings to private. You can change access settings by clicking on the three dots in the upper right corner, and click on "Settings" and then "Manage Access". 

If the project board does not comply with the Contribution Guidelines provided here, the Trusted Committer can request changes from the creator of the Project board, and ultimately delete the board, in a lack of response or compliance. 

  
### Using the Backlog Project Boards
The collaborative Project boards are set up with a specific layout intended to make it easier to interact in i-t. 
  
#### Board View
The boards has a backlog/Scrum-board style view, with status columns. The boards additionally has a _"By Priority"_ list-view, sorting issues according to a `priority` attribute, that must be specified manually for each issue. Participants of this repository can use views as they like, or alternatively create new views in the boards if they want to, by clicking "+ New View" in the upper right corner of the view tabs. 
  
<img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/projects_new_view.png" width="70%"/>

**Note**: 
- Issues can only be sorted according to certain attributes specified by the creator of the Project board, who, including the [Trusted Committer]s, are the only ones with the access to change/add to these attributes. If you want to add a new attribute, you should post your suggestion in [Discussions]. 
- Remember to save your new view, by clicking the small arrow with the blue dot, and click "Save changes". 
    
  <img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/contribution_save_view.png" width="30%"/>
  
#### Card Columns
There are four columns in the Backlog project boards:

- **Backlog**: Issues that are planned and defined, but work on the issue is not yet initiated.
- **In Progress**: The issue or pull request is currently being worked on, and sees some sort of progress.
- **Waiting/Blocked**: The issue or pull request waits for external inputs, review or something else to be finished/clarified, before work can continue.
- **Done**: The work on the issue is done. 
  - At the [Weekly Synchronous Sesssions] the "Done" column is emptied. 

### Draft issues 
In the Project boards, it is possible to use the "draft issue" feature, by clicking on the "+ New Item" in the bottom of a column.

<img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/projects_add_card.png" width="70%"/>
  
The draft issues should only be used with the purpose of informing the community of certain work that needs to be done, or link to relevant background information, but that is not further defined, and that you haven't started working on yet. It is highly encouraged to use the discussions forum instead, for ideas that need to be refined, or creating an actual issue for well-defined tasks, to allow for interaction in the issue, which is otherwise not possible in the "draft" mode.  

 

## Discussions
[Discussions] are intended for open-form ideas or topics, and creates a space for the community to interact and communicate more broadly than is possible in the more closed-form issues and pull requests. 

If a discussion turns into a well-defined feature or task and/or sees a large connect from the community, participants of the community can create issues and pull requests. If a specific discussion has been especially valuable for the arguments exchanged or the options discarded etc. or a specific good formulation is present feel free to link to said comment or discussion in the pull request/issue. Such written discussion artifacts are considered passive documentation and reduce writing burden and can reduce disconnection from the past leading to history repeating. The guidelines for using the [Discussions] tab will be provided in the following. 


### Creating a discussion
Before creating a new discussion, make sure to check if there already exists a post on your topic. To create a discussion, navigate to this repository's [Discussions] and click on the green button "New Discussion". In the "Select Category" drop-down menu, click on the most appropriate category for your discussion. The purpose and guidelines for each discussion channel will be provided in the following.
  
### General
The "General" channel is a forum for open, informal discussion in the community. As long as your post complies with our [Code of Conduct](linkhere), you are free to post whatever you would like to discuss with the rest of the community in this channel. 

### Announcements
The "Announcement" channel can be used to announce decisions made in Project-Origin, or major features that you are working on, that will affect a larger part of the community. If you rather want to highlight something that you and/or someone else have made, you can post it in the [Show and Tell](#show-and-tell) channel instead. 
 
### Ideas
If you have an idea for something, but you don’t know exactly if or how to implement it, you can post your idea in the "Ideas" discussion channel. This discussion channel gives the community a place to discuss loose, open ideas, and potentially develop an idea until it becomes a well-defined feature request. If an idea evolves to a feature request, an issue can be created for this.

If you create a post, make sure to provide a self-explanatory, concise title, and describe the idea in an easy-to-understand way, providing at least
1. A description of the idea
2. The motivation behind the idea - What can be gained with your idea in Project-Origin? 
3. Which parts of the idea you want to process in collaboration with the community, for the idea to become a feature request.

### Polls
Polls will be posted in this discussion forum. You can create polls to evaluate the community's interest in your ideas, or discover the direction that gets the most connect from the community, for something you want to add. Anyone with read access to this repository can create polls, vote in polls, and view the poll's results. 

#### Guidelines for polls 
If you are creating a poll, be aware of the following guidelines: 

- Polls require a question and at least two options. 
- You can add a maximum of eight options. 
- You must specify a **default option**, which will be applied if no votes are received.
- You must add an answering deadline, to let the community know when the poll will be closed. 
  - The deadline must be at least 10 working days, excluding rare exceptions. See the [Decision-Making Policy] for more information. 
  - The decision-making in polls follows a lazy consensus approach, inspired by the [Apache definition](https://community.apache.org/committers/lazyConsensus.html) of the concept. A poll can have a status of proposed, open or closed. When a poll is closed, the outcome of the vote indicated in the poll can be considered as decided on. 
- Editing a poll will reset any votes that have already been made. 

If you are voting on a poll, note that you cannot change your vote, after submitting it. If you need some clarification before providing your answer, provide your clarification request as a comment in the discussion thread of the poll. 
    
When you reach your answering deadline you should
    
1. Select "Lock conversation" in the column on the right side of your poll. 
2. Add a comment stating that the deadline is reached and select "Close with comment". 
    - You also have the option to "Close as outdated" if your poll is not relevant anymore, or "Close as duplicate" if you find that someone else made a poll on the same topic

    <img src="https://github.com/project-origin/origin-collaboration/blob/main/docs/figures/contribution_close_poll.png" width="70%"/>

### Q & A
The "Q & A" channel creates a space for the community to ask each other questions, and support each other on specific problems. 
  
As the discussion author, you can mark a comment as "the answer" to your question. In the upper-right corner of the comment you want to mark as the answer, click on the check-mark in the upper right corner of the comment. 
  
When you mark a question as an answer, GitHub will highlight the comment and replies to the comment to help visitors quickly find the answer, when navigating to your discussion post. (Note that you can't mark a threaded comment in response to a comment as the answer to a discussion. You also can't mark a minimized comment as the answer to a discussion). 

### Show and Tell
The "Show and Tell" channel is a place to highlight and praise contributions that you, or someone else have made. Make sure to mention any contributors of the feature that you want to highlight in your discussion post. 

<!-- Anchorlink style -->
[Pull Request]: https://github.com/project-origin/origin-collaboration/pulls 
[Discussions]: https://github.com/project-origin/origin-collaboration/discussions
[Issues]: https://github.com/project-origin/origin-collaboration/issues
[Issue Templates]: https://github.com/project-origin/origin-collaboration/issues/new/choose
[Projects]: https://github.com/project-origin/origin-collaboration/projects?query=is%3Aopen
[Trusted Committer]: https://github.com/project-origin/origin-collaboration/blob/main/docs/introductory/innersource-short-role-descriptions.md#the-trusted-committer
[Pull Request Templates]: https://github.com/project-origin/origin-collaboration/tree/main/pull_request_templates
[RFC]: https://github.com/project-origin/origin-collaboration/blob/main/docs/additional_reading/rfc.md
[Meeting Minutes]: https://github.com/project-origin/origin-collaboration/tree/main/meeting_minutes
[Trusted Committer of this repository]: https://github.com/project-origin/origin-collaboration
[Projects Guide]: https://github.com/project-origin/origin-collaboration/blob/main/docs/github_guides/projects.md
[Decision-Making Policy]: https://github.com/project-origin/origin-collaboration/blob/main/docs/guidelines/decision_making_policy.md
[Project-Origin Backlog]: https://github.com/orgs/project-origin/projects/6
[Collaboration Platform Backlog]: https://github.com/orgs/project-origin/projects/2/views/1
[First Workshop in Aarhus 8.11.2022]: https://github.com/orgs/project-origin/projects/5/views/1 
[Weekly Synchronous Sesssions]: https://github.com/project-origin/origin-collaboration#weekly-synchronous-sessions
