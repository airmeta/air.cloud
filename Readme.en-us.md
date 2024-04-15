# Air.Cloud

    Aiming to provide a set of framework conventions to help you build large-scale applications more quickly

### Concepts:

    Standard: Abstract class/interface class implementing a certain function
    Module: Class library implementing a certain standard
    Plugin: Lightweight module, generally used in Web services, necessary implementation for non-Web service operation. 
            For example, JWT authentication does not affect the actual operation of your business without this function.

### Background:
        
        Today, as the demand for large-scale Internet applications is gradually popularized, the traditional three-tier architecture can no longer meet the current system requirements.
        Issues such as traffic, speed, and security are becoming increasingly prominent. We hope for a framework with a high degree of customization by developers, achieving the goal of small horses pulling small carts, and big horses pulling big carts.

### Q&A Session

#### What is Air.Cloud?

        Air.Cloud includes a set of framework standards and some basic implementations. You can build your service by implementing these standards or using libraries that implement these standards.
        This will bring you a Lego-like experience, building a dynamic framework that can be large or small through the combination of different standard implementation libraries, and using this framework to build your service.

#### Why does Air.Cloud include the Furion framework?

    There are indeed some advantages of the Furion framework that are worth our learning, absorbing the strengths of all parties and continuously improving our content.

#### Will this become a patchwork monster?

       No, not at all. Air.Cloud itself does not implement functions, it includes a set of standards and some basic functions.
    You can imagine it as a socket full of interfaces. We promote framework updates and iterations by defining standards.

       We make full use of Furion's loading mechanism to load modules, and introduce implemented modules through dependency injection. This is usually decided by you, the users of Air.Cloud, to decide which version or which open source participant's standard implementation to reference.

        If you don't want to use these implementations, you can completely write your own large modules inside and outside the Air.Cloud framework, and promote them to more Air.Cloud users through nuget.

        If you are a package knight, then the default standard implementations are enough for your daily use.
        If you are a wheel monster, then you can write your own standard implementation and share it with other Air.Cloud users.
        

#### Why is Air.Cloud so flexible?

    The author himself has used many back-end frameworks of all sizes, but my real feeling is the following two points:

        1. Small frameworks are quick to write, but many functions are not supported and need to be tinkered with by yourself.
        2. Large frameworks are slow to write, often a small function or service requires more time and energy to maintain.
    
    For example:
        Large frameworks are like giving you a cup of water with various substances added, you need to filter it before you can drink it, delete some useless components/injections to achieve the best "drinking" effect. 
        Small frameworks are like giving you oxygen and hydrogen, you need to turn it into the water you need to "drink" yourself.
        So, what about Air.Cloud? 
        This situation was considered in the design. We neither give you a cup of water with various substances added, nor give you the raw materials to make water, but give you various types of water for you to choose from. What kind of water you want to drink and how much water you want to drink is up to you.

        Each standard implementation is a module, and this module will be published on nuget for you to choose from. Nuget is like a large shelf, you just need to pick the water you want from it to "drink"
