Experimenting with Spec-Driven AI development for a personal project while GH Copilot AI wasn't too expensive.
- GH Copilot
- Spec first, implement next workflow.
- Custom agents for various jobs. Jobs are handed down from requirement gathering, planning, designing, to implementation and analysis agents.
- Experimenting with agents, instruction, and skill files.

Overview:
Didn't trust AI to solely decide everything across the whole project, I've already had some experience in implementing a C# command system for Unity from an old project, so I had a good idea of the architecture and features required which allowed me to guide AI effectively.
On this occasion I reinforced correct behaviour of AI with mostly just unit tests, and tested the rest using a simple sample that I made in Unity. AI performed pretty decent when given a strongly outlined project, hard goals, and clear direction, however reviewing AI generated plans and designs was
a definite bore, so much so that eventually you half heartedly just agree with what AI wrote. I ended up skimming the plans making sure they look okay with nothing that stands out as a major concern and then allowing AI to start implementing. Came to a conclusion (atleast for this project, and as a software engineer)
that its easier to review the code in diff format (over plans) and see if it that matches to what I would have done if I were the one asked to implement the feature myself.

Was a fun experiment, sitting back and letting it do most of the job, at the end of the day I would not be comfortable with letting this be production code without more serious intervention from myself, but I think there is a sweet spot for AI + Human programming (pair programming).
